using System.Text;
using Cesium.Core;
using Cesium.Core.Warnings;
using Yoakke.Streams;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Text;
using static Cesium.Preprocessor.CPreprocessorTokenType;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Preprocessor;

public record CPreprocessor(
    string CompilationUnitPath,
    ILexer<IToken<CPreprocessorTokenType>> Lexer,
    IIncludeContext IncludeContext,
    IMacroContext MacroContext,
    IWarningProcessor WarningProcessor)
{
    private readonly MacroExpansionEngine _macroExpansion = new(WarningProcessor, MacroContext);

    public async Task<string> ProcessSource()
    {
        var buffer = new StringBuilder();
        await foreach (var t in GetPreprocessingResults())
        {
            buffer.Append(t.Text);
        }

        return buffer.ToString();
    }

    private async IAsyncEnumerable<IToken<CPreprocessorTokenType>> GetPreprocessingResults()
    {
        var file = ParsePreprocessingFile();
        await foreach (var token in ProcessGroup(file.Group))
        {
            yield return token;
        }
    }

    private PreprocessingFile ParsePreprocessingFile()
    {
        using var transactionalLexer = new TransactionalLexer(Lexer.ToEnumerableUntilEnd(), WarningProcessor);
        var parser = new CPreprocessorParser(transactionalLexer);
        var file = parser.ParsePreprocessingFile();
        if (file.IsError)
        {
            RaisePreprocessorParseError(file.Error);
        }

        return file.Ok;
    }

    private async IAsyncEnumerable<IToken<CPreprocessorTokenType>> ProcessGroup(IEnumerable<IGroupPart> group)
    {
        foreach (var part in group)
        {
            var tokens = ProcessGroupPart(part);
            await foreach (var token in tokens)
            {
                yield return token;
            }
        }
    }

    private async IAsyncEnumerable<IToken<CPreprocessorTokenType>> ProcessGroupPart(IGroupPart groupPart)
    {
        switch (groupPart)
        {
            case IncludeDirective include:
            {
                var filePathToken = include.Tokens.Single();
                var filePath = filePathToken.Text;
                var includeFilePath = LookUpIncludeFile(filePath);
                if (!IncludeContext.ShouldIncludeFile(includeFilePath))
                {
                    yield break;
                }

                using var reader = IncludeContext.OpenFileStream(includeFilePath);
                if (reader == null)
                {
                    throw new PreprocessorException(
                        filePathToken.Location,
                        $"Cannot find file {filePath} for include directive. Include context: {IncludeContext}");
                }
                await foreach (var token in ProcessInclude(includeFilePath, reader))
                {
                    yield return token;
                }

                break;
            }
            case ErrorDirective error:
            {
                string errorText;
                if (error.Tokens is null)
                {
                    errorText = "#error";
                }
                else
                {
                    var errorBuilder = new StringBuilder();
                    foreach (var token in error.Tokens ?? [])
                    {
                        errorBuilder.Append(token.Text);
                    }

                    errorText = errorBuilder.ToString();
                }

                throw new PreprocessorException(error.Location, errorText.Trim());
            }
            case DefineDirective define:
            {
                var macroName = define.Identifier.Text;
                MacroContext.DefineMacro(macroName, define.Parameters, define.Replacement);

                break;
            }
            case UnDefDirective undef:
            {
                var identifier = GetSingleToken([undef.Identifier], PreprocessingToken).Text;
                MacroContext.UndefineMacro(identifier);
                break;
            }
            case IfSection ifSection:
            {
                var (ifGroup, elIfGroups, elseGroup) = ifSection;
                List<GuardedGroup> conditionalGroups = [ifGroup, ..elIfGroups];
                foreach (var group in conditionalGroups)
                {
                    var condition = group.Clause ??
                        throw new PreprocessorException(group.Keyword.Location, $"Empty condition in group {group}");
                    var expression = ParseExpression(condition);
                    var keyword = group.Keyword.Text;
                    var shouldWrapInDefined = keyword is "ifdef" or "ifndef" or "elifdef" or "elifndef";
                    if (shouldWrapInDefined)
                        expression = WrapIntoDefined(expression);
                    var evaluationResult = EvaluateExpression(expression);
                    var isPositive = keyword is "if" or "ifdef" or "elif" or "elifdef";
                    var isNegative = keyword is "ifndef" or "elifndef";
                    if (!isPositive && !isNegative)
                        throw new PreprocessorException(
                            group.Keyword.Location,
                            $"Unknown conditional directive {keyword}.");

                    if ((evaluationResult && isPositive) || (!evaluationResult && isNegative)) // the first one wins
                    {
                        await foreach (var token in ProcessGroup(group.Tokens))
                        {
                            yield return token;
                        }
                        yield break;
                    }
                }

                if (elseGroup == null) break;

                await foreach (var token in ProcessGroup(elseGroup.Tokens))
                {
                    yield return token;
                }
                break;
            }
            case PragmaDirective pragma:
            {
                var identifier = pragma.Tokens?.FirstOrDefault()?.Text;
                if (identifier == "once")
                {
                    IncludeContext.RegisterGuardedFileInclude(CompilationUnitPath);
                }
                else if (identifier == "pinvoke")
                {
                    if (pragma.Tokens == null)
                        throw new PreprocessorException(pragma.Location, $"Bad pragma: {pragma}");
                    var tokens = pragma.Tokens?.Where(_ => _.Kind != WhiteSpace)!;
                    var type = tokens.ElementAt(2);
                    // start: pinvoke [0] ( [1] "lib name" [2] ) [3]
                    // end:   pinvoke [0] ( [1] end        [2] ) [3]
                    // with prefix: pinvoke [0] ( [1] "lib name" [2] , [3] prefix [4] ) [5]

                    foreach(var tok in TokenizeString($"_Pragma(pinvoke, {type!.Text}{(tokens.Count() > 5 ? $",{tokens.ElementAt(4).Text}" : null)})"))
                        yield return tok;
                }
                break;
            }
            case EmptyDirective:
                break;
            case TextLineBlock textLine:
                foreach (var token in _macroExpansion.ExpandMacros(textLine.Tokens))
                {
                    yield return token;
                }
                var newLine = new Token<CPreprocessorTokenType>(new Range(), new Location(), "\n", NewLine);
                yield return newLine;
                break;
            case NonDirective nonDirective:
                throw new PreprocessorException(
                    nonDirective.Location,
                    "Preprocessor execution of a non-directive was requested.");
            default:
                SourceLocationInfo location = groupPart.Location;
                var groupName = groupPart.Keyword switch
                {
                    {} kw => kw.Text + " ",
                    null => ""
                };
                throw new WipException(
                    77,
                    $"{location}: Preprocessor directive {groupName}is not supported, yet.");
        }

        yield break;

        IToken<CPreprocessorTokenType> GetSingleToken(
            IEnumerable<IToken<CPreprocessorTokenType>> tokens,
            CPreprocessorTokenType type)
        {
            var token = tokens.Single();
            if (token.Kind != type)
            {
                throw new PreprocessorException(
                    token.Location,
                    $"Cannot process preprocessor directive: expected {type}, " +
                    $"but got {token.Kind} {token.Text}.");
            }

            return token;
        }
    }

    private IPreprocessorExpression ParseExpression(IEnumerable<IToken<CPreprocessorTokenType>> tokens)
    {
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
            tokens.Union(new[]
            {
                new Token<CPreprocessorTokenType>(
                    new Range(),
                    new Location(),
                    "",
                    End)
            })).ToBuffered();
        var p = new CPreprocessorExpressionParser(stream);
        var expression = p.ParseExpression();
        if (expression.IsError)
        {
            RaisePreprocessorParseError(expression.Error);
        }

        if (!(stream.IsEnd || stream.Peek().Kind == End))
            throw new AssertException("(stream.IsEnd || stream.Peek().Kind == End) is not true.");

        return expression.Ok.Value;
    }

    /// <summary>Performs wrapping, e.g. <code>FOO</code> -&gt; <code></code>.</summary>
    private static DefinedExpression WrapIntoDefined(IPreprocessorExpression expression)
    {
        if (expression is IdentifierExpression identifier)
        {
            return new(expression.Location, identifier.Identifier);
        }

        throw new PreprocessorException(
            expression.Location,
            $"Definition check expects an identifier, but a complex expression found: {expression}.");
    }

    private bool EvaluateExpression(IPreprocessorExpression expression)
    {
        var macroExpression = expression.EvaluateExpression(MacroContext);
        var includeTokens = macroExpression.AsBoolean(expression.Location);
        return includeTokens;
    }

    private string LookUpIncludeFile(string filePath) => filePath[0] switch
    {
        '<' => IncludeContext.LookUpAngleBracedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        '"' => IncludeContext.LookUpQuotedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        _ => throw new Exception($"Unknown kind of include file path: {filePath}.")
    };

    private async IAsyncEnumerable<IToken<CPreprocessorTokenType>> ProcessInclude(
        string compilationUnitPath,
        TextReader fileReader)
    {
        var lexer = new CPreprocessorLexer(new SourceFile(compilationUnitPath, fileReader));
        var subProcessor = this with { CompilationUnitPath = compilationUnitPath, Lexer = lexer };
        await foreach (var item in subProcessor.GetPreprocessingResults())
        {
            yield return item;
        }

        yield return new Token<CPreprocessorTokenType>(new Range(), new Location(), "\n", NewLine);
    }

    private static IEnumerable<IToken<CPreprocessorTokenType>> TokenizeString(string code)
    {
        var tokenizer = new CPreprocessorLexer("<null>", code);
        return tokenizer.ToEnumerableUntilEnd();
    }

    internal static void RaisePreprocessorParseError(ParseError error)
    {
        var got = error.Got switch
        {
            IToken token => token.Text,
            { } item => item.ToString(),
            null => "<no token>"
        };

        var errorMessage = new StringBuilder($"Error during preprocessing. Found {got}, ");
        if (error.Elements.Count == 1)
        {
            errorMessage.Append($"but expected {ExpectedString(error.Elements.Single())}");
        }
        else
        {
            errorMessage.Append("but expected one of:");
            foreach (var item in error.Elements)
            {
                errorMessage.AppendLine($"\n- {ExpectedString(item)}");
            }
        }

        var location = error.Position as SourceLocationInfo ?? new SourceLocationInfo("<unknown>", null, null);
        throw new PreprocessorException(location, errorMessage.ToString());

        static string ExpectedString(KeyValuePair<string, ParseErrorElement> element) =>
            string.Join(", ", element.Value.Expected) + $" (rule {element.Key})";
    }
}

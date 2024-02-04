using System.Globalization;
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

    private IEnumerable<IToken<CPreprocessorTokenType>> ExpandMacros(
        IEnumerable<IToken<CPreprocessorTokenType>> tokens)
    {
        // TODO[#537]: Test for passing a macro name into another macro.

        using var lexer = new TransactionalLexer(tokens, WarningProcessor);
        while (!lexer.IsEnd)
        {
            var token = lexer.Consume();
            if (token.Kind == PreprocessingToken)
            {
                var macroName = token.Text;
                if (!MacroContext.TryResolveMacro(macroName, out var parameters, out var replacement))
                {
                    yield return token;
                    continue;
                }

                var maybeArguments = ParseArguments(parameters, lexer);
                if (maybeArguments is not {} arguments)
                {
                    // Not a macro call, just emit the token.
                    yield return token;
                    continue;
                }

                if (arguments.IsError)
                    RaisePreprocessorParseError(arguments.Error);

                foreach (var replaced in ReplaceMacro(token, arguments.Ok, replacement))
                {
                    yield return replaced;
                }
            }
            else
            {
                yield return token;
            }
        }
    }

    /// <returns><c>null</c> ⇒ do not expand, non-<c>null</c> ⇒ expand if ok, throw error if not ok.</returns>
    private ParseResult<MacroArguments>? ParseArguments(MacroParameters? parameters, TransactionalLexer lexer)
    {
        using var transaction = lexer.BeginTransaction();

        if (parameters == null)
        {
            MacroArguments emptyResult = new(new(), new());
            return transaction.End<MacroArguments>(ParseResult.Ok(emptyResult, 0));
        }

        if (lexer.IsEnd)
        {
            // A macro has some parameters, but we are at the end of the token stream, so no arguments is available. We
            // should just skip expanding this macro.
            var location = lexer.LastToken?.Location
                           ?? new SourceLocationInfo(CompilationUnitPath, null, null);
            transaction.End(ParseResult.Error(",", null, location, "macro arguments"));
            return null;
        }

        if (Consume() is var leftBrace and not { Kind: LeftParen })
        {
            SourceLocationInfo location = leftBrace.Location;
            transaction.End(ParseResult.Error(",", leftBrace, location, "macro arguments"));
            return null; // no braces at all means we should just skip expanding this macro
        }

        var namedArguments = new Dictionary<string, List<IToken<CPreprocessorTokenType>>>();
        var isFirstArgument = true;
        foreach (var parameterToken in parameters.Parameters)
        {
            if (isFirstArgument)
            {
                isFirstArgument = false;
            }
            else if (Consume() is var comma and not { Text: "," or ")" })
            {
                SourceLocationInfo location = comma.Location;
                return transaction.End(ParseResult.Error(",", comma, location, "macro arguments"));
            }

            var name = parameterToken.Text;
            var argument = ParseArgument(lexer);
            if (argument.IsError)
                return transaction.End(argument.Error);

            namedArguments[name] = argument.Ok;
        }

        var varArgs = new List<List<IToken<CPreprocessorTokenType>>>();
        if (parameters.HasEllipsis)
        {
            while (Peek() is not { Kind: RightParen })
            {
                if (isFirstArgument)
                {
                    isFirstArgument = false;
                }
                else if (Consume() is var comma and not { Text: "," })
                {
                    SourceLocationInfo location = comma.Location;
                    return transaction.End(ParseResult.Error(",", comma, location, "macro arguments"));
                }

                var varArg = ParseArgument(lexer);
                if (varArg.IsError)
                    return transaction.End(varArg.Error);

                varArgs.Add(varArg.Ok);
            }
        }

        if (Consume() is var token and not { Kind: RightParen })
        {
            SourceLocationInfo location = token.Location;
            return transaction.End(ParseResult.Error(")", token, location, "macro arguments"));
        }

        var result = new MacroArguments(namedArguments, varArgs);
        return transaction.End<MacroArguments>(ParseResult.Ok(result, 0));

        IToken<CPreprocessorTokenType> Consume()
        {
            IToken<CPreprocessorTokenType> currentToken;
            do
            {
                currentToken = lexer.Consume();
            } while (currentToken is { Kind: WhiteSpace or Comment });

            return currentToken;
        }

        IToken<CPreprocessorTokenType> Peek()
        {
            IToken<CPreprocessorTokenType> currentToken;
            var index = 0;
            do
            {
                currentToken = lexer.Peek(index++);
            } while (currentToken is { Kind: WhiteSpace or Comment });

            return currentToken;
        }
    }

    private ParseResult<List<IToken<CPreprocessorTokenType>>> ParseArgument(TransactionalLexer lexer)
    {
        using var transaction = lexer.BeginTransaction();

        if (lexer.IsEnd)
            return ParseResult.Error("argument", "end of stream", 0, "macro argument");

        SourceLocationInfo argumentStartLocation = lexer.Peek().Location;
        var argument = new List<IToken<CPreprocessorTokenType>>();
        while (!lexer.IsEnd && lexer.Peek() is not ({ Kind: RightParen } or { Text: "," }))
        {
            var token = lexer.Consume();
            argument.Add(token);
            if (token is { Kind: LeftParen })
            {
                var tail = ParseNestedParenthesesBlock(token.Location);
                if (!tail.IsOk)
                    return transaction.End(tail.Error);
            }
        }

        if (lexer.IsEnd)
            return transaction.End(ParseResult.Error(") or ,", null, argumentStartLocation, "macro argument"));

        var processedArgument = TrimStartingWhitespace(ExpandMacros(argument)).ToList();
        return transaction.End<List<IToken<CPreprocessorTokenType>>>(ParseResult.Ok(processedArgument, 0));

        ParseResult<object?> ParseNestedParenthesesBlock(SourceLocationInfo start)
        {
            while (!lexer.IsEnd && lexer.Peek() is not { Kind: RightParen })
            {
                var token = lexer.Consume();
                argument.Add(token);
                if (token is { Kind: LeftParen })
                {
                    var tail = ParseNestedParenthesesBlock(token.Location);
                    if (!tail.IsOk)
                        return tail;
                }
            }

            if (lexer.IsEnd)
                return ParseResult.Error("terminated macro argument", null, start, "macro argument nested parentheses block");

            var rightParen = lexer.Consume();
            argument.Add(rightParen);
            return ParseResult.Ok<object?>(null, 0);
        }

        IEnumerable<IToken<CPreprocessorTokenType>> TrimStartingWhitespace(
            IEnumerable<IToken<CPreprocessorTokenType>> tokens) =>
            tokens.SkipWhile(t => t is { Kind: WhiteSpace or Comment or NewLine });
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReplaceMacro(
        IToken macroNameToken,
        MacroArguments arguments,
        IList<IToken<CPreprocessorTokenType>> replacement)
    {
        switch (macroNameToken.Text)
        {
            case "__FILE__":
                yield return new Token<CPreprocessorTokenType>(
                    macroNameToken.Range,
                    macroNameToken.Location,
                    "\"" + macroNameToken
                        .Location
                        .File?
                        .Path
                        .Replace("\\", "\\\\") + "\"",
                    PreprocessingToken);
                yield break;
            case "__LINE__":
            {
                var line = macroNameToken.Location.Range.Start.Line + 1;
                yield return new Token<CPreprocessorTokenType>(
                    macroNameToken.Range,
                    macroNameToken.Location,
                    line.ToString(CultureInfo.InvariantCulture),
                    PreprocessingToken);
                yield break;
            }
        }

        if (replacement.Count > 0)
            replacement = ExpandMacros(replacement).ToList();

        using var lexer = new TransactionalLexer(replacement, WarningProcessor);
        var spaceBuffer = new List<IToken<CPreprocessorTokenType>>();
        while (!lexer.IsEnd)
        {
            var token = lexer.Consume();
            switch (token)
            {
                case { Kind: WhiteSpace }:
                    spaceBuffer.Add(token);
                    break;
                case { Text: "#" or "##" } when PeekSignificant() is { Kind: PreprocessingToken }:
                {
                    var next = ConsumeSignificant();
                    var sequence = ProcessTokenNoHash(next);
                    switch (token.Text)
                    {
                        case "#":
                            foreach (var space in spaceBuffer)
                            {
                                yield return space;
                            }
                            spaceBuffer.Clear();
                            yield return new Token<CPreprocessorTokenType>(
                                next.Range,
                                next.Location,
                                Stringify(sequence),
                                PreprocessingToken);
                            break;
                        case "##":
                            // TODO: Figure out what to do if the sequence is more than one item.
                            spaceBuffer.Clear();
                            yield return new Token<CPreprocessorTokenType>(
                                next.Range,
                                next.Location,
                                sequence.Single().Text,
                                PreprocessingToken);
                            break;
                        default:
                            throw new PreprocessorException(token.Location, $"Unexpected token \"{token.Text}.");
                    }
                    break;
                }
                default:
                {
                    foreach (var space in spaceBuffer)
                    {
                        yield return space;
                    }
                    spaceBuffer.Clear();

                    var sequence = ProcessTokenNoHash(token);
                    foreach (var item in sequence)
                    {
                        yield return item;
                    }
                    break;
                }
            }
        }

        foreach (var space in spaceBuffer)
        {
            yield return space;
        }
        yield break;

        IToken<CPreprocessorTokenType>? PeekSignificant()
        {
            for (var i = 0; !lexer.IsEnd; ++i)
            {
                var currentToken = lexer.Peek(i);
                if (currentToken is not { Kind: WhiteSpace or Comment or NewLine })
                    return currentToken;
            }

            return null;
        }

        IToken<CPreprocessorTokenType> ConsumeSignificant()
        {
            IToken<CPreprocessorTokenType> currentToken;
            do
            {
                currentToken = lexer.Consume();
            } while (currentToken is { Kind: WhiteSpace or Comment or NewLine });

            return currentToken;
        }

        IEnumerable<IToken<CPreprocessorTokenType>> ProcessTokenNoHash(IToken<CPreprocessorTokenType> token)
        {
            switch (token)
            {
                case { Kind: PreprocessingToken, Text: var text }
                    when arguments.Named.TryGetValue(text, out var argument):
                {
                    // macro argument substitution
                    foreach (var argumentToken in argument)
                    {
                        yield return argumentToken;
                    }

                    break;
                }
                case { Kind: PreprocessingToken, Text: "__VA_ARGS__" }:
                {
                    var isFirst = true;
                    foreach (var varArg in arguments.VarArg)
                    {
                        // Comma before each but the first.
                        if (isFirst) isFirst = false;
                        else
                        {
                            yield return new Token<CPreprocessorTokenType>(
                                new Range(),
                                new Location(),
                                ",",
                                Separator);
                        }

                        foreach (var argToken in varArg)
                            yield return argToken;
                    }

                    break;
                }
                default:
                    yield return token;
                    break;
            }
        }

        string Stringify(IEnumerable<IToken<CPreprocessorTokenType>> tokens)
        {
            // According to the standard, and whitespace sequence gets replaced with a single space.
            var replaced = ReplaceWhitespace();
            var builder = new StringBuilder("\"");
            foreach (var token in replaced)
            {
                foreach (var c in token.Text)
                {
                    builder.Append(c switch
                    {
                        '\\' or '\"' => "\\" + c,
                        _ => c.ToString()
                    });
                }
            }
            builder.Append('"');
            return builder.ToString();

            IEnumerable<IToken<CPreprocessorTokenType>> ReplaceWhitespace()
            {
                var spaceEater = false;
                foreach (var token in tokens)
                {
                    if (token is { Kind: WhiteSpace or Comment or NewLine })
                    {
                        if (spaceEater) continue;

                        spaceEater = true;
                        var spaceToken = token.Text == " " ? token : new Token<CPreprocessorTokenType>(
                            token.Range,
                            token.Location,
                            " ",
                            WhiteSpace);
                        yield return spaceToken;
                    }
                    else
                    {
                        spaceEater = false;
                        yield return token;
                    }
                }
            }
        }
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
                        $"Cannot find file {filePath} for include directive.");
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

                break;
            }
            case EmptyDirective:
                break;
            case TextLine textLine:
                foreach (var token in ExpandMacros(textLine.Tokens ?? []))
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

    private void RaisePreprocessorParseError(ParseError error)
    {
        var got = error.Got switch
        {
            IToken token => token.Text,
            { } item => item.ToString(),
            null => "<no token>"
        };

        var errorMessage = new StringBuilder($"Error during preprocessing file \"{CompilationUnitPath}\", {error.Position}. Found {got}, ");
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

        var location = error.Position as SourceLocationInfo ?? new SourceLocationInfo(CompilationUnitPath, null, null);
        throw new PreprocessorException(location, errorMessage.ToString());

        static string ExpectedString(KeyValuePair<string, ParseErrorElement> element) =>
            string.Join(", ", element.Value.Expected) + $" (rule {element.Key})";
    }

    private record MacroArguments(
        Dictionary<string, List<IToken<CPreprocessorTokenType>>> Named,
        List<List<IToken<CPreprocessorTokenType>>> VarArg
    );
}

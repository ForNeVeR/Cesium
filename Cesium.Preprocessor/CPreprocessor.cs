using System.Text;
using Cesium.Core;
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
    IMacroContext MacroContext)
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
        using var transactionalLexer = new TransactionalLexer(Lexer);
        var parser = new CPreprocessorParser(transactionalLexer);
        var file = parser.ParsePreprocessingFile();
        if (file.IsError)
        {
            RaisePreprocessorParseError(file.Error);
        }

        return file.Ok;
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReplaceMacro(
        IToken<CPreprocessorTokenType> macroNameToken,
        IStream<IToken<CPreprocessorTokenType>> stream)
    {
        if (MacroContext.TryResolveMacro(macroNameToken.Text, out var parameters, out var tokenReplacement))
        {
            Dictionary<string, List<IToken<CPreprocessorTokenType>>> replacement = new();
            if (parameters is not null)
            {
                if (parameters.Parameters.Length > 0 || parameters.HasEllipsis)
                {
                    var parameterIndex = -1;
                    var openParensCount = 0;
                    var hitOpenToken = false;
                    List<IToken<CPreprocessorTokenType>> currentParameter = new();

                    if (parameters.HasEllipsis)
                    {
                        replacement.Add("__VA_ARGS__", new());
                    }

                    do
                    {
                        var parametersParsingToken = stream.Consume();
                        switch (parametersParsingToken)
                        {
                            case { Kind: LeftParen }:
                                if (openParensCount != 0)
                                {
                                    currentParameter.Add(parametersParsingToken);
                                }

                                hitOpenToken = true;
                                openParensCount++;
                                if (parameterIndex == -1)
                                {
                                    parameterIndex = 0;
                                }
                                break;
                            case { Kind: RightParen }:
                                openParensCount--;
                                if (openParensCount != 0)
                                {
                                    currentParameter.Add(parametersParsingToken);
                                }
                                break;
                            case { Kind: Separator, Text: "," }:
                                if (parameterIndex == -1)
                                {
                                    throw new PreprocessorException(
                                        parametersParsingToken.Location,
                                        $"Expected '(' but got {parametersParsingToken.Kind} " +
                                        $"{parametersParsingToken.Text} at range {parametersParsingToken.Range}.");
                                }

                                if (openParensCount == 1)
                                {
                                    if (parameters.Parameters.Length > parameterIndex)
                                    {
                                        replacement.Add(parameters.GetName(parameterIndex), currentParameter);
                                        parameterIndex++;
                                    }
                                    else if (parameters.HasEllipsis)
                                    {
                                        if (replacement.TryGetValue("__VA_ARGS__", out var vaArgs))
                                        {
                                            vaArgs.AddRange(currentParameter);
                                            vaArgs.Add(new Token<CPreprocessorTokenType>(
                                                macroNameToken.Range,
                                                macroNameToken.Location,
                                                ",",
                                                Separator));
                                        }
                                    }
                                    else
                                    {
                                        throw new PreprocessorException(
                                            parametersParsingToken.Location,
                                            $"The function {macroNameToken.Text} defined" +
                                            $" at {parametersParsingToken.Range} has more" +
                                            " parameters than the macro allows.");
                                    }

                                    currentParameter = new();
                                }
                                else
                                {
                                    currentParameter.Add(parametersParsingToken);
                                }
                                break;
                            default:
                                if (openParensCount == 0 && parametersParsingToken.Kind == WhiteSpace)
                                {
                                    continue;
                                }

                                currentParameter.Add(parametersParsingToken);
                                break;
                        }
                    }
                    while (openParensCount > 0 || !hitOpenToken);


                    if (parameters.Parameters.Length > parameterIndex)
                    {
                        replacement.Add(parameters.GetName(parameterIndex), currentParameter);
                    }
                    else
                    {
                        if (replacement.TryGetValue("__VA_ARGS__", out var vaArgs))
                        {
                            vaArgs.AddRange(currentParameter);
                        }
                    }
                }
                else
                {
                    var openParensCount = 0;

                    do
                    {
                        var parametersParsingToken = stream.Consume();
                        switch (parametersParsingToken)
                        {
                            case { Kind: LeftParen }:
                                openParensCount++;
                                break;
                            case { Kind: RightParen }:
                                openParensCount--;
                                break;
                        }
                    } while (openParensCount > 0);
                }
            }
            else // an object-like macro
            {
                if (macroNameToken.Text == "__FILE__")
                {
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
                }

                if (macroNameToken.Text == "__LINE__")
                {
                    var line = macroNameToken.Location.Range.Start.Line + 1;
                    yield return new Token<CPreprocessorTokenType>(
                        macroNameToken.Range,
                        macroNameToken.Location,
                        line.ToString(),
                        PreprocessingToken);
                    yield break;
                }
            }

            foreach (var parameter in replacement.Values)
                TrimMacroArgument(parameter);

            var performStringReplace = false;
            var includeNextVerbatim = false;
            var nestedStream = new EnumerableStream<IToken<CPreprocessorTokenType>>(tokenReplacement);
            var pendingWhitespaces = new List<Token<CPreprocessorTokenType>>();
            while (!nestedStream.IsEnd)
            {
                var subToken = nestedStream.Consume();
                if (subToken is { Kind: Hash })
                {
                    performStringReplace = true;
                    continue;
                }

                if (subToken is { Kind: DoubleHash })
                {
                    includeNextVerbatim = true;
                    continue;
                }

                if (subToken is { Kind: PreprocessingToken })
                {
                    if (!includeNextVerbatim)
                    {
                        foreach (var whitespaceToken in pendingWhitespaces)
                        {
                            yield return whitespaceToken;
                        }

                        pendingWhitespaces.Clear();
                    }

                    if (replacement.TryGetValue(subToken.Text, out var parameterTokens))
                    {
                        if (includeNextVerbatim)
                        {
                            foreach (var parameterToken in parameterTokens)
                            {
                                yield return new Token<CPreprocessorTokenType>(
                                    macroNameToken.Range,
                                    macroNameToken.Location,
                                    parameterToken.Text,
                                    parameterToken.Kind);
                            }
                        }
                        else if (performStringReplace)
                        {
                            var stringValue = string.Join(string.Empty, parameterTokens.Select(t => t.Text));
                            var escapedStringValue = stringValue
                                .Replace("\\", "\\\\")
                                .Replace("\"", "\\\"");
                            escapedStringValue = $"\"{escapedStringValue}\"";
                            yield return new Token<CPreprocessorTokenType>(
                                macroNameToken.Range,
                                macroNameToken.Location,
                                escapedStringValue,
                                macroNameToken.Kind);
                            performStringReplace = false;
                        }
                        else
                        {
                            foreach (var parameterToken in parameterTokens)
                            {
                                yield return new Token<CPreprocessorTokenType>(
                                    macroNameToken.Range,
                                    macroNameToken.Location,
                                    parameterToken.Text,
                                    parameterToken.Kind);
                            }
                        }
                    }
                    else
                    {
                        if (includeNextVerbatim)
                        {
                            yield return subToken;
                        }
                        else
                        {
                            foreach (var nestedT in ReplaceMacro(subToken, nestedStream))
                            {
                                yield return nestedT;
                            }
                        }
                    }

                    includeNextVerbatim = false;
                }
                else if (subToken is { Kind: WhiteSpace })
                {
                    if (!includeNextVerbatim)
                    {
                        pendingWhitespaces.Add(new Token<CPreprocessorTokenType>(
                            macroNameToken.Range,
                            macroNameToken.Location,
                            subToken.Text,
                            subToken.Kind));
                    }
                }
                else
                {
                    yield return new Token<CPreprocessorTokenType>(
                        macroNameToken.Range,
                        macroNameToken.Location,
                        subToken.Text,
                        subToken.Kind);
                }
            }
        }
        else
        {
            yield return macroNameToken;
        }

        yield break;

        void TrimMacroArgument(IList<IToken<CPreprocessorTokenType>> parameter)
        {
            while (parameter.FirstOrDefault() is { Kind: WhiteSpace or NewLine })
            {
                parameter.RemoveAt(0);
            }

            while (parameter.LastOrDefault() is { Kind: WhiteSpace or NewLine })
            {
                parameter.RemoveAt(parameter.Count - 1);
            }
        }
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReplaceMacrosInLine(TextLine line)
    {
        var tokens = line.Tokens ?? [];
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(tokens);
        while (!stream.IsEnd)
        {
            var token = stream.Consume();
            if (token.Kind == PreprocessingToken)
            {
                foreach (var subToken in ReplaceMacro(token, stream))
                {
                    yield return subToken;
                }
            }
            else
            {
                yield return token;
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

                throw new PreprocessorException(error.DirectiveStart, errorText.Trim());
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
            case TextLine textLine:
                foreach (var token in ReplaceMacrosInLine(textLine))
                {
                    yield return token;
                }
                var newLine = new Token<CPreprocessorTokenType>(new Range(), new Location(), "\n", NewLine);
                yield return newLine;
                break;
            default:
                throw new WipException(
                    77,
                    $"Preprocessor directive not supported: {groupPart}.");
            // TODO: Include the group part name token into each the group name, for ease of identification, and include the name in this error message, together with the source information.
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
        var subProcessor = new CPreprocessor(compilationUnitPath, lexer, IncludeContext, MacroContext);
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

        var location = error.Position as ErrorLocationInfo ?? new ErrorLocationInfo(CompilationUnitPath, null, null);
        throw new PreprocessorException(location, errorMessage.ToString());

        static string ExpectedString(KeyValuePair<string, ParseErrorElement> element) =>
            string.Join(", ", element.Value.Expected) + $" (rule {element.Key})";
    }

    internal static void EmitWarning(string text)
    {
        Console.Error.WriteLine(text);
    }
}

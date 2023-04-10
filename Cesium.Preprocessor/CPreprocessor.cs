using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Cesium.Core;
using Yoakke.Streams;
using Yoakke.SynKit.Lexer;
using static Cesium.Preprocessor.CPreprocessorTokenType;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Preprocessor;

public record CPreprocessor(string CompilationUnitPath, ILexer<IToken<CPreprocessorTokenType>> Lexer, IIncludeContext IncludeContext, IMacroContext MacroContext)
{
    private bool IncludeTokens = true;
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
        var newLine = true;

        var stream = Lexer.ToStream();
        while (!stream.IsEnd)
        {
            var token = stream.Consume();
            switch (token.Kind)
            {
                case End:
                    yield break;

                case WhiteSpace:
                case Comment:
                    if (IncludeTokens)
                    {
                        yield return token;
                    }
                    break;

                case NewLine:
                    newLine = true;
                    if (IncludeTokens)
                    {
                        yield return token;
                    }
                    break;

                case Hash:
                    if (newLine)
                    {
                        foreach (var t in await ProcessDirective(ReadDirectiveLine(token, stream)))
                            yield return t;

                        // Leave newLine as true, since we've processed the directive at the previous line, so now we're
                        // necessarily at the start of a new one.
                    }
                    else
                    {
                        yield return token;
                    }
                    break;

                case Error:
                case DoubleHash:
                case HeaderName:
                case Separator:
                case LeftParen:
                case RightParen:
                    newLine = false;
                    if (IncludeTokens)
                    {
                        yield return token;
                    }
                    break;
                case PreprocessingToken:
                    newLine = false;
                    if (IncludeTokens)
                    {
                        if (MacroContext.TryResolveMacro(token.Text, out var parameters, out var tokenReplacement))
                        {
                            Dictionary<string, List<IToken<CPreprocessorTokenType>>> replacement = new();
                            if (parameters is not null)
                            {
                                int parameterIndex = -1;
                                int openParensCount = 0;
                                List<IToken<CPreprocessorTokenType>> currentParameter = new();
                                IToken<CPreprocessorTokenType> parametersParsingToken;
                                do
                                {
                                    parametersParsingToken = stream.Consume();
                                    switch (parametersParsingToken)
                                    {
                                        case { Kind: LeftParen }:
                                            if (openParensCount != 0)
                                            {
                                                currentParameter.Add(parametersParsingToken);
                                            }

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
                                                throw new PreprocessorException($"Expected '(' but got {parametersParsingToken.Kind} {parametersParsingToken.Text} at range {parametersParsingToken.Range}.");
                                            }

                                            if (openParensCount == 1)
                                            {
                                                replacement.Add(parameters[parameterIndex], currentParameter);
                                                currentParameter = new();
                                                parameterIndex++;
                                            }
                                            else
                                            {
                                                currentParameter.Add(parametersParsingToken);
                                            }
                                            break;
                                        default:
                                            currentParameter.Add(parametersParsingToken);
                                            break;
                                    }
                                }
                                while (openParensCount > 0);
                                replacement.Add(parameters[parameterIndex], currentParameter);
                            }

                            bool performStringReplace = false;
                            foreach (var subToken in tokenReplacement)
                            {
                                if (subToken is { Kind: Hash })
                                {
                                    performStringReplace = true;
                                    continue;
                                }

                                if (subToken is { Kind: PreprocessingToken } && replacement.TryGetValue(subToken.Text, out var parameterTokens))
                                {
                                    if (performStringReplace)
                                    {
                                        var stringValue = string.Join(string.Empty, parameterTokens.Select(t => t.Text));
                                        var escapedStringValue = stringValue.Replace("\\", "\\\\")
                                            .Replace("\"", "\\\"")
                                            ;
                                        escapedStringValue = $"\"{escapedStringValue}\"";
                                        yield return new Token<CPreprocessorTokenType>(token.Range, escapedStringValue, token.Kind);
                                        performStringReplace = false;
                                    }
                                    else
                                    {
                                        foreach (var parameterToken in parameterTokens)
                                        {
                                            yield return new Token<CPreprocessorTokenType>(token.Range, parameterToken.Text, parameterToken.Kind);
                                        }
                                    }
                                }
                                else
                                {
                                    yield return new Token<CPreprocessorTokenType>(token.Range, subToken.Text, subToken.Kind);
                                }
                            }
                        }
                        else
                        {
                            yield return token;
                        }
                    }
                    break;

                default:
                    throw new PreprocessorException($"Illegal token {token.Kind} {token.Text}.");
            }
        }
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReadDirectiveLine(
        IToken<CPreprocessorTokenType> firstToken,
        IStream<IToken<CPreprocessorTokenType>> stream)
    {
        yield return firstToken;
        foreach (var token in ReadTillEnd(stream))
        {
            yield return token;
        }
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReadTillEnd(
        IStream<IToken<CPreprocessorTokenType>> stream)
    {
        while (!stream.IsEnd)
        {
            var token = stream.Consume();
            switch (token.Kind)
            {
                case NewLine:
                case End:
                    yield break;
                default:
                    yield return token;
                    break;
            }
        }
    }

    private async ValueTask<IEnumerable<IToken<CPreprocessorTokenType>>> ProcessDirective(
        IEnumerable<IToken<CPreprocessorTokenType>> directiveTokens)
    {
        using var enumerator = directiveTokens.GetEnumerator();

        int? line = null;
        IToken<CPreprocessorTokenType> ConsumeNext(params CPreprocessorTokenType[] allowedTypes)
        {
            bool moved;
            while ((moved = enumerator.MoveNext()) && enumerator.Current is { Kind: WhiteSpace })
            {
                // Skip any whitespace in between tokens.
            }

            if (!moved)
                throw new PreprocessorException(
                    "Preprocessing directive too short at line " +
                    $"{line?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}.");

            var token = enumerator.Current;
            if (allowedTypes.Contains(token.Kind)) return enumerator.Current;

            var expectedTypeString = string.Join(" or ", allowedTypes);
            throw new PreprocessorException(
                $"Cannot process preprocessor directive: expected {expectedTypeString}, " +
                $"but got {token.Kind} {token.Text} at {token.Range.Start}.");
        }

        IEnumerable<IToken<CPreprocessorTokenType>> ConsumeLine()
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is { Kind: WhiteSpace })
                {
                    continue;
                }

                yield return enumerator.Current;
            }
        }

        var hash = ConsumeNext(Hash);
        line = hash.Range.Start.Line;

        var keyword = ConsumeNext(PreprocessingToken);
        switch (keyword.Text)
        {
            case "include":
            {
                var filePath = ConsumeNext(HeaderName).Text;
                var includeFilePath = LookUpIncludeFile(filePath);
                if (!IncludeContext.CanIncludeFile(includeFilePath))
                {
                    return Array.Empty<IToken<CPreprocessorTokenType>>();
                }

                using var reader = IncludeContext.OpenFileStream(includeFilePath);
                var tokensList = new List<IToken<CPreprocessorTokenType>>();
                await foreach (var token in ProcessInclude(includeFilePath, reader))
                {
                    tokensList.Add(token);
                }

                bool hasRemaining;
                while ((hasRemaining = enumerator.MoveNext())
                       && enumerator.Current is { Kind: WhiteSpace })
                {
                    // eat remaining whitespace
                }

                if (hasRemaining && enumerator.Current is var t and not { Kind: WhiteSpace })
                    throw new PreprocessorException($"Invalid token after include path: {t.Kind} {t.Text}");

                return tokensList;
            }
            case "error":
            {
                var errorText = new StringBuilder();
                while (enumerator.MoveNext())
                {
                    errorText.Append(enumerator.Current.Text);
                }
                throw new PreprocessorException($"Error: {errorText.ToString().Trim()}");
            }
            case "define":
            {
                var expressionTokens = ConsumeLine();
                var (macroDefinition, replacement) = EvaluateMacroDefinition(expressionTokens.ToList());
                MacroContext.DefineMacro(macroDefinition.Name, macroDefinition.Parameters, replacement);
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "ifdef":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool includeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
                IncludeTokens = includeTokens;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "if":
            {
                var expressionTokens = ConsumeLine();
                bool includeTokens = EvaluateExpression(expressionTokens.ToList());
                IncludeTokens = includeTokens;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "ifndef":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool donotIncludeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
                IncludeTokens = !donotIncludeTokens;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "endif":
            {
                IncludeTokens = true;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "else":
            {
                IncludeTokens = !IncludeTokens;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "pragma":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                if (identifier == "once")
                {
                    IncludeContext.RegisterPragmaOnceFile(CompilationUnitPath);
                    return Array.Empty<IToken<CPreprocessorTokenType>>();
                }
                else
                {
                    throw new WipException(
                        77,
                        $"Preprocessor #pragma directive not supported: {keyword.Kind} {keyword.Text}.");
                }
            }
            default:
                throw new WipException(
                    77,
                    $"Preprocessor directive not supported: {keyword.Kind} {keyword.Text}.");
        }
    }
    private bool EvaluateExpression(IEnumerable<IToken<CPreprocessorTokenType>> expressionTokens)
    {
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
            expressionTokens.Union(new[] { new Token<CPreprocessorTokenType>(new Range(), "", End) })).ToBuffered();
        var p = new CPreprocessorExpressionParser(stream);
        var expression = p.ParseExpression();
        var macroExpression = expression.Ok.Value.EvaluateExpression(MacroContext);
        bool includeTokens = macroExpression != null;
        return includeTokens;
    }
    private (CPreprocessorMacroDefinitionParser.MacroDefinition, List<IToken<CPreprocessorTokenType>>) EvaluateMacroDefinition(IEnumerable<IToken<CPreprocessorTokenType>> expressionTokens)
    {
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
            expressionTokens.Union(new[] { new Token<CPreprocessorTokenType>(new Range(), "", End) })).ToBuffered();
        var p = new CPreprocessorMacroDefinitionParser(stream);
        var macroDefinition = p.ParseMacro();
        var macroReplacement = new List<IToken<CPreprocessorTokenType>>();
        while (!stream.IsEnd)
        {
            var token = stream.Consume();
            if (token is not { Kind: End })
            {
                macroReplacement.Add(token);
            }
        }

        return (macroDefinition.Ok.Value, macroReplacement);
    }

    private string LookUpIncludeFile(string filePath) => filePath[0] switch
    {
        '<' => IncludeContext.LookUpAngleBracedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        '"' => IncludeContext.LookUpQuotedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        _ => throw new Exception($"Unknown kind of include file path: {filePath}.")
    };

    private async IAsyncEnumerable<IToken<CPreprocessorTokenType>> ProcessInclude(string compilationUnitPath, TextReader fileReader)
    {
        var lexer = new CPreprocessorLexer(fileReader);
        var subProcessor = new CPreprocessor(compilationUnitPath, lexer, IncludeContext, MacroContext);
        await foreach (var item in subProcessor.GetPreprocessingResults())
        {
            yield return item;
        }

        yield return new Token<CPreprocessorTokenType>(new Range(), "\n", NewLine);
    }
}

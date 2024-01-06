using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Cesium.Core;
using Yoakke.Streams;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;
using static Cesium.Preprocessor.CPreprocessorTokenType;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Preprocessor;

public record CPreprocessor(string CompilationUnitPath, ILexer<IToken<CPreprocessorTokenType>> Lexer, IIncludeContext IncludeContext, IMacroContext MacroContext)
{
    private bool IncludeTokens => _includeTokensStack.All(includeToken => includeToken);
    private readonly Stack<bool> _includeTokensStack = new();
    public async Task<string> ProcessSource()
    {
        var buffer = new StringBuilder();
        await foreach (var t in GetPreprocessingResults())
        {
            buffer.Append(t.Text);
        }

        return buffer.ToString();
    }

    private void PushIncludeTokensDepth(bool includeTokes)
    {
        _includeTokensStack.Push(includeTokes);
    }

    private void PopIncludeTokensDepth()
    {
        _includeTokensStack.Pop();
    }

    private void SwitchIncludeTokensDepth()
    {
        var lastItem = _includeTokensStack.Pop();
        this.PushIncludeTokensDepth(!lastItem);
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
                    {
                        newLine = false;
                        if (IncludeTokens)
                        {
                            foreach (var producedToken in ReplaceMacro(token, stream))
                            {
                                yield return producedToken;
                            }
                        }
                    }
                    break;

                default:
                    throw new PreprocessorException($"Illegal token {token.Kind} {token.Text}.");
            }
        }
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReplaceMacro(IToken<CPreprocessorTokenType> token, IStream<IToken<CPreprocessorTokenType>> stream)
    {
        if (MacroContext.TryResolveMacro(token.Text, out var macroDefinition, out var tokenReplacement))
        {
            Dictionary<string, List<IToken<CPreprocessorTokenType>>> replacement = new();
            if (macroDefinition is FunctionMacroDefinition functionMacro)
            {
                if (functionMacro.Parameters is { } parameters
                    && (parameters.Length > 0 || functionMacro.hasEllipsis))
                {
                    int parameterIndex = -1;
                    int openParensCount = 0;
                    bool hitOpenToken = false;
                    List<IToken<CPreprocessorTokenType>> currentParameter = new();
                    IToken<CPreprocessorTokenType> parametersParsingToken;

                    if (functionMacro.hasEllipsis)
                    {
                        replacement.Add("__VA_ARGS__", new());
                    }

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
                                    throw new PreprocessorException($"Expected '(' but got {parametersParsingToken.Kind} {parametersParsingToken.Text} at range {parametersParsingToken.Range}.");
                                }

                                if (openParensCount == 1)
                                {
                                    if (parameters.Length > parameterIndex)
                                    {
                                        replacement.Add(parameters[parameterIndex], currentParameter);
                                        parameterIndex++;
                                        currentParameter = new();
                                    }
                                    else if (functionMacro.hasEllipsis)
                                    {
                                        if (replacement.TryGetValue("__VA_ARGS__", out var va_args))
                                        {
                                            va_args.AddRange(currentParameter);
                                            va_args.Add(new Token<CPreprocessorTokenType>(token.Range, token.Location, ",", CPreprocessorTokenType.Separator));
                                        }
                                    }
                                    else
                                    {
                                        throw new PreprocessorException($"The function {functionMacro.Name} defined at {parametersParsingToken.Range} has more parameters than the macro allows.");
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


                    if (parameters.Length > parameterIndex)
                    {
                        replacement.Add(parameters[parameterIndex], currentParameter);
                    }
                    else
                    {
                        if (replacement.TryGetValue("__VA_ARGS__", out var va_args_))
                        {
                            va_args_.AddRange(currentParameter);
                        }
                    }
                }
                else
                {
                    IToken<CPreprocessorTokenType> parametersParsingToken;
                    int openParensCount = 0;

                    do
                    {
                        parametersParsingToken = stream.Consume();
                        switch (parametersParsingToken)
                        {
                            case { Kind: LeftParen }:
                                openParensCount++;
                                break;
                            case { Kind: RightParen }:
                                openParensCount--;
                                break;
                            default: break;
                        }
                    } while (openParensCount > 0);
                }

            }
            if (macroDefinition is ObjectMacroDefinition objectMacro)
            {
                if (objectMacro.Name == "__FILE__")
                {
                    yield return new Token<CPreprocessorTokenType>(token.Range, token.Location, "\"" + token.Location.File?.Path + "\"", PreprocessingToken);
                    yield break;
                }

                if (token.Text == "__LINE__")
                {
                    var line = token.Location.Range.Start.Line;
                    yield return new Token<CPreprocessorTokenType>(
                        token.Range,
                        token.Location,
                        line.ToString(),
                        PreprocessingToken);
                    yield break;
                }
            }
            bool performStringReplace = false;
            bool includeNextVerbatim = false;
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
                                yield return new Token<CPreprocessorTokenType>(token.Range, token.Location, parameterToken.Text, parameterToken.Kind);
                            }
                        }
                        else if (performStringReplace)
                        {
                            var stringValue = string.Join(string.Empty, parameterTokens.Select(t => t.Text));
                            var escapedStringValue = stringValue.Replace("\\", "\\\\")
                                .Replace("\"", "\\\"")
                                ;
                            escapedStringValue = $"\"{escapedStringValue}\"";
                            yield return new Token<CPreprocessorTokenType>(token.Range, token.Location, escapedStringValue, token.Kind);
                            performStringReplace = false;
                        }
                        else
                        {
                            foreach (var parameterToken in parameterTokens)
                            {
                                yield return new Token<CPreprocessorTokenType>(token.Range, token.Location, parameterToken.Text, parameterToken.Kind);
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
                        pendingWhitespaces.Add(new Token<CPreprocessorTokenType>(token.Range, token.Location, subToken.Text, subToken.Kind));
                    }
                }
                else
                {
                    yield return new Token<CPreprocessorTokenType>(token.Range, token.Location, subToken.Text, subToken.Kind);
                }
            }
        }
        else
        {
            yield return token;
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

    private static IEnumerable<IToken<CPreprocessorTokenType>> ReadTillEnd(
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
                case NextLine:
                    token = stream.Consume();
                    bool nextLineReached = false;
                    while (token.Kind == CPreprocessorTokenType.NewLine || token.Kind == CPreprocessorTokenType.WhiteSpace)
                    {
                        if (token.Kind == CPreprocessorTokenType.NewLine)
                        {
                            nextLineReached = true;
                        }

                        token = stream.Consume();
                    }

                    if (!nextLineReached)
                        throw new PreprocessorException($"Illegal token {token.Kind} {token.Text} after \\.");

                    yield return token;
                    break;
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

        IEnumerable<IToken<CPreprocessorTokenType>> ConsumeLineAll()
        {
            while (enumerator.MoveNext())
            {
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
                // If in disabled block, do not attempt to include files.
                var filePath = ConsumeNext(HeaderName).Text;
                var tokensList = new List<IToken<CPreprocessorTokenType>>();
                if (IncludeTokens)
                {
                    var includeFilePath = LookUpIncludeFile(filePath);
                    if (!IncludeContext.ShouldIncludeFile(includeFilePath))
                    {
                        return Array.Empty<IToken<CPreprocessorTokenType>>();
                    }

                    if (!File.Exists(includeFilePath))
                    {
                        Console.Error.WriteLine($"Cannot find path to {filePath} during parsing {CompilationUnitPath}");
                    }

                    using var reader = IncludeContext.OpenFileStream(includeFilePath);
                    await foreach (var token in ProcessInclude(includeFilePath, reader))
                    {
                        tokensList.Add(token);
                    }
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

                if (IncludeTokens)
                    throw new PreprocessorException($"Error: {errorText.ToString().Trim()}");

                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "define":
            {
                var expressionTokens = ConsumeLineAll();
                if (IncludeTokens)
                {
                    var (macroDefinition, replacement) = EvaluateMacroDefinition(expressionTokens.ToList());
                    MacroContext.DefineMacro(macroDefinition.Name, macroDefinition, replacement);
                }

                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "undef":
            {
                var expressionTokens = ConsumeLineAll();
                if (IncludeTokens)
                {
                    var (macroDefinition, replacement) = EvaluateMacroDefinition(expressionTokens.ToList());
                    MacroContext.UndefineMacro(macroDefinition.Name);
                }

                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "ifdef":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool includeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
                PushIncludeTokensDepth(includeTokens);
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "if":
            {
                var expressionTokens = ConsumeLine();
                bool includeTokens = EvaluateExpression(expressionTokens.ToList());
                PushIncludeTokensDepth(includeTokens);
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "ifndef":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool donotIncludeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
                PushIncludeTokensDepth(!donotIncludeTokens);
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "endif":
            {
                PopIncludeTokensDepth();
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "else":
            {
                SwitchIncludeTokensDepth();
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "pragma":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                if (identifier == "once")
                {
                    IncludeContext.RegisterGuardedFileInclude(CompilationUnitPath);
                }

                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            default:
                throw new WipException(
                    77,
                    $"Preprocessor directive not supported: {keyword.Kind} {keyword.Text}.");
        }
    }
    private bool EvaluateExpression(IList<IToken<CPreprocessorTokenType>> expressionTokens)
    {
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
            expressionTokens.Union(new[] { new Token<CPreprocessorTokenType>(new Range(), new Yoakke.SynKit.Text.Location(), "", End) })).ToBuffered();
        var p = new CPreprocessorExpressionParser(stream);
        var expression = p.ParseExpression();
        if (expression.IsError)
        {
            throw new PreprocessorException($"Cannot parse {(expression.Error.Elements.FirstOrDefault().Key)}, got {expression.Error.Got}");
        }

        var macroExpression = expression.Ok.Value.EvaluateExpression(MacroContext);
        Debug.Assert(stream.IsEnd || stream.Peek().Kind == CPreprocessorTokenType.End);
        bool includeTokens = macroExpression.AsBoolean();
        return includeTokens;
    }
    private static (MacroDefinition, List<IToken<CPreprocessorTokenType>>) EvaluateMacroDefinition(IEnumerable<IToken<CPreprocessorTokenType>> expressionTokens)
    {
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
            expressionTokens.Union(new[] { new Token<CPreprocessorTokenType>(new Range(), new Yoakke.SynKit.Text.Location(), "", End) })).ToBuffered();
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

        if (macroDefinition.IsError)
        {
            throw new PreprocessorException($"Cannot parse macro definition. Expected: {string.Join(",", macroDefinition.Error.Elements.Values.Select(_ => $"{_.Context},{string.Join(",", _.Expected)}"))} got: {macroDefinition.Error.Got}");
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
        var lexer = new CPreprocessorLexer(new SourceFile(compilationUnitPath, fileReader));
        var subProcessor = new CPreprocessor(compilationUnitPath, lexer, IncludeContext, MacroContext);
        await foreach (var item in subProcessor.GetPreprocessingResults())
        {
            yield return item;
        }

        yield return new Token<CPreprocessorTokenType>(new Range(), new Yoakke.SynKit.Text.Location(), "\n", NewLine);
    }
}

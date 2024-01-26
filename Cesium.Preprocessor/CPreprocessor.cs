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
                    IToken<CPreprocessorTokenType> parametersParsingToken;

                    if (parameters.HasEllipsis)
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
                                    throw new PreprocessorException(
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
                                        if (replacement.TryGetValue("__VA_ARGS__", out var va_args))
                                        {
                                            va_args.AddRange(currentParameter);
                                            va_args.Add(new Token<CPreprocessorTokenType>(
                                                macroNameToken.Range,
                                                macroNameToken.Location,
                                                ",",
                                                Separator));
                                        }
                                    }
                                    else
                                    {
                                        throw new PreprocessorException(
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
                        if (replacement.TryGetValue("__VA_ARGS__", out var va_args_))
                        {
                            va_args_.AddRange(currentParameter);
                        }
                    }
                }
                else
                {
                    IToken<CPreprocessorTokenType> parametersParsingToken;
                    var openParensCount = 0;

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
                        }
                    } while (openParensCount > 0);
                }
            }
            if (parameters is null) // an object-like macro
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
                    var nextLineReached = false;
                    while (token.Kind is NewLine or WhiteSpace)
                    {
                        if (token.Kind == NewLine)
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
        IToken<CPreprocessorTokenType> GetSingleToken(
            IEnumerable<IToken<CPreprocessorTokenType>> tokens,
            CPreprocessorTokenType type)
        {
            var token = tokens.Single();
            if (token.Kind != type)
            {
                throw new PreprocessorException(
                    $"Cannot process preprocessor directive: expected {type}, " +
                    $"but got {token.Kind} {token.Text} at {token.Location}.");
            }

            return token;
        }

        // TODO: Clean this up / remove
        //
        // IEnumerable<IToken<CPreprocessorTokenType>> ConsumeLine()
        // {
        //     while (enumerator.MoveNext())
        //     {
        //         if (enumerator.Current is { Kind: WhiteSpace })
        //         {
        //             continue;
        //         }
        //
        //         yield return enumerator.Current;
        //     }
        // }
        //
        // IEnumerable<IToken<CPreprocessorTokenType>> ConsumeLineAll()
        // {
        //     while (enumerator.MoveNext())
        //     {
        //         yield return enumerator.Current;
        //     }
        // }
        //
        // ConditionalElementResult GetIfInBlockForElif()
        // {
        //     var ifConditionInBlock = _includeTokensStack
        //         .FirstOrDefault(i => _conditionalBlockInitialWords.Contains(i.KeyWord));
        //     if (ifConditionInBlock is null)
        //         throw new PreprocessorException($"Directive such as an elif cannot exist" +
        //                                         $" without a directive such as if");
        //     if (_includeTokensStack.Count > 0 && ifConditionInBlock.UpperFlag is null)
        //         throw new PreprocessorException($"Not the first {string.Join(',', _conditionalBlockInitialWords)}" +
        //                                         $" blocks can't be without" +
        //                                         $"{nameof(ConditionalElementResult.UpperFlag)}");
        //     return ifConditionInBlock;
        // }
        //
        // bool ArePreviousConditionalsFalse()
        // {
        //     return _includeTokensStack
        //         .TakeWhileWithLastInclude(i =>
        //             !_conditionalBlockInitialWords.Contains(i.KeyWord))
        //         .All(i => !i.Flag);
        // }
        //
        // var hash = ConsumeNext(Hash);
        // line = hash.Range.Start.Line;
        // var preprocessorToken = ConsumeNext(PreprocessingToken);

        switch (groupPart)
        {
            case IncludeDirective include:
            {
                var filePath = include.Tokens.Single().Text;
                var includeFilePath = LookUpIncludeFile(filePath);
                if (!IncludeContext.ShouldIncludeFile(includeFilePath))
                {
                    yield break;
                }

                if (!File.Exists(includeFilePath))
                {
                    EmitWarning($"Cannot find path to {filePath} during parsing {CompilationUnitPath}");
                    yield break;
                }

                using var reader = IncludeContext.OpenFileStream(includeFilePath);
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
                    // TODO: Add error location.
                    errorText = $"[error at {CompilationUnitPath}]";
                }
                else
                {
                    var errorBuilder = new StringBuilder();
                    foreach (var token in error.Tokens ?? [])
                    {
                        // TODO: Test for error with spaces and embedded comments
                        errorBuilder.Append(token.Text);
                    }

                    errorText = errorBuilder.ToString();
                }

                throw new PreprocessorException($"Error: {errorText.Trim()}");
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
                                    throw new PreprocessorException($"Empty condition in group {group}");
                    var evaluationResult = EvaluateExpression(condition);
                    var keyword = group.Keyword.Text;
                    var isPositive = keyword is "if" or "ifdef" or "elif" or "elifdef";
                    var isNegative = keyword is "ifndef" or "elifndef";
                    if (!isPositive && !isNegative)
                        throw new PreprocessorException($"Unknown conditional directive {keyword}.");

                    if ((evaluationResult && isPositive) || (!evaluationResult && isNegative)) // the first one wins
                    {
                        await foreach (var token in ProcessGroup(group.Tokens))
                        {
                            yield return token;
                        }
                        break;
                    }
                }

                if (elseGroup == null) yield break;

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
                foreach (var token in textLine.Tokens ?? [])
                {
                    yield return token;
                }
                break;
            default:
                throw new WipException(
                    77,
                    $"Preprocessor directive not supported: {groupPart}.");
            // TODO: Include the group part name token into each the group name, for ease of identification, and include the name in this error message, together with the source information.
        }
    }

    private bool EvaluateExpression(IEnumerable<IToken<CPreprocessorTokenType>> expressionTokens)
    {
        var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
            expressionTokens.Union(new[]
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

        var macroExpression = expression.Ok.Value.EvaluateExpression(MacroContext);

        if (!(stream.IsEnd || stream.Peek().Kind == End))
            throw new AssertException("(stream.IsEnd || stream.Peek().Kind == End) is not true.");

        bool includeTokens = macroExpression.AsBoolean();
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
        var errorMessage = new StringBuilder($"Error during preprocessing file \"{CompilationUnitPath}\", {error.Position}.");
        foreach (var item in error.Elements.Values)
        {
            errorMessage.AppendLine($"\n- {item.Context}: expected {string.Join(", ", item.Expected)}");
        }

        throw new PreprocessorException(errorMessage.ToString());
    }

    private static void EmitWarning(string text)
    {
        Console.Error.WriteLine(text);
    }
}

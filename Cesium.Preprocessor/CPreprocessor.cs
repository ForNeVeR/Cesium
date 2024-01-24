using System.Text;
using Cesium.Core;
using Yoakke.Streams;
using Yoakke.SynKit.Lexer;
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

        foreach (var group in file.Groups)
        {
            switch (group)
            {
                default: throw new WipException(WipException.ToDo, $"Preprocessing group not supported: {group}.");
            }
        }

        yield break; // TODO: Remove this line.
    }

    private PreprocessingFile ParsePreprocessingFile()
    {
        using var transactionalLexer = new TransactionalLexer(Lexer);
        var parser = new CPreprocessorParser(transactionalLexer);
        var file = parser.ParsePreprocessingFile();
        if (file.IsError)
        {
            throw file.Error.Got switch
            {
                IToken<CPreprocessorTokenType> token => new PreprocessorException(
                    $"Error during preprocessing file \"{CompilationUnitPath}\"." +
                    $"Error at position {file.Error.Position}. Got {token.Text}."),
                var other => new PreprocessorException(
                    $"Error during preprocessing file \"{CompilationUnitPath}\"." +
                    $"Error at position {file.Error.Position}. Got {other}."),
            };
        }

        return file.Ok;
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ReplaceMacro(
        IToken<CPreprocessorTokenType> token,
        IStream<IToken<CPreprocessorTokenType>> stream)
    {
        if (MacroContext.TryResolveMacro(token.Text, out var macroDefinition, out var tokenReplacement))
        {
            Dictionary<string, List<IToken<CPreprocessorTokenType>>> replacement = new();
            if (macroDefinition is FunctionMacroDefinition functionMacro)
            {
                if (functionMacro.Parameters is { } parameters
                    && (parameters.Length > 0 || functionMacro.hasEllipsis))
                {
                    var parameterIndex = -1;
                    var openParensCount = 0;
                    var hitOpenToken = false;
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
                                    throw new PreprocessorException(
                                        $"Expected '(' but got {parametersParsingToken.Kind} " +
                                        $"{parametersParsingToken.Text} at range {parametersParsingToken.Range}.");
                                }

                                if (openParensCount == 1)
                                {
                                    if (parameters.Length > parameterIndex)
                                    {
                                        replacement.Add(parameters[parameterIndex], currentParameter);
                                        parameterIndex++;
                                    }
                                    else if (functionMacro.hasEllipsis)
                                    {
                                        if (replacement.TryGetValue("__VA_ARGS__", out var va_args))
                                        {
                                            va_args.AddRange(currentParameter);
                                            va_args.Add(new Token<CPreprocessorTokenType>(
                                                token.Range,
                                                token.Location,
                                                ",",
                                                Separator));
                                        }
                                    }
                                    else
                                    {
                                        throw new PreprocessorException(
                                            $"The function {functionMacro.Name} defined" +
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
            if (macroDefinition is ObjectMacroDefinition objectMacro)
            {
                if (objectMacro.Name == "__FILE__")
                {
                    yield return new Token<CPreprocessorTokenType>(
                        token.Range,
                        token.Location,
                        "\"" + token
                            .Location
                            .File?
                            .Path
                            .Replace("\\", "\\\\") + "\"",
                        PreprocessingToken);
                    yield break;
                }

                if (token.Text == "__LINE__")
                {
                    var line = token.Location.Range.Start.Line + 1;
                    yield return new Token<CPreprocessorTokenType>(
                        token.Range,
                        token.Location,
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
                                    token.Range,
                                    token.Location,
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
                                token.Range,
                                token.Location,
                                escapedStringValue,
                                token.Kind);
                            performStringReplace = false;
                        }
                        else
                        {
                            foreach (var parameterToken in parameterTokens)
                            {
                                yield return new Token<CPreprocessorTokenType>(
                                    token.Range,
                                    token.Location,
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
                            token.Range,
                            token.Location,
                            subToken.Text,
                            subToken.Kind));
                    }
                }
                else
                {
                    yield return new Token<CPreprocessorTokenType>(
                        token.Range,
                        token.Location,
                        subToken.Text,
                        subToken.Kind);
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

    // private async ValueTask<IEnumerable<IToken<CPreprocessorTokenType>>> ProcessDirective(
    //     IEnumerable<IToken<CPreprocessorTokenType>> directiveTokens)
    // {
    //     using var enumerator = directiveTokens.GetEnumerator();
    //
    //     int? line = null;
    //     IToken<CPreprocessorTokenType> ConsumeNext(params CPreprocessorTokenType[] allowedTypes)
    //     {
    //         bool moved;
    //         while ((moved = enumerator.MoveNext()) && enumerator.Current is { Kind: WhiteSpace })
    //         {
    //             // Skip any whitespace in between tokens.
    //         }
    //
    //         if (!moved)
    //             throw new PreprocessorException(
    //                 "Preprocessing directive too short at line " +
    //                 $"{line?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}.");
    //
    //         var token = enumerator.Current;
    //         if (allowedTypes.Contains(token.Kind)) return enumerator.Current;
    //
    //         var expectedTypeString = string.Join(" or ", allowedTypes);
    //         throw new PreprocessorException(
    //             $"Cannot process preprocessor directive: expected {expectedTypeString}, " +
    //             $"but got {token.Kind} {token.Text} at {token.Range.Start}.");
    //     }
    //
    //     IEnumerable<IToken<CPreprocessorTokenType>> ConsumeLine()
    //     {
    //         while (enumerator.MoveNext())
    //         {
    //             if (enumerator.Current is { Kind: WhiteSpace })
    //             {
    //                 continue;
    //             }
    //
    //             yield return enumerator.Current;
    //         }
    //     }
    //
    //     IEnumerable<IToken<CPreprocessorTokenType>> ConsumeLineAll()
    //     {
    //         while (enumerator.MoveNext())
    //         {
    //             yield return enumerator.Current;
    //         }
    //     }
    //
    //     ConditionalElementResult GetIfInBlockForElif()
    //     {
    //         var ifConditionInBlock = _includeTokensStack
    //             .FirstOrDefault(i => _conditionalBlockInitialWords.Contains(i.KeyWord));
    //         if (ifConditionInBlock is null)
    //             throw new PreprocessorException($"Directive such as an elif cannot exist" +
    //                                             $" without a directive such as if");
    //         if (_includeTokensStack.Count > 0 && ifConditionInBlock.UpperFlag is null)
    //             throw new PreprocessorException($"Not the first {string.Join(',', _conditionalBlockInitialWords)}" +
    //                                             $" blocks can't be without" +
    //                                             $"{nameof(ConditionalElementResult.UpperFlag)}");
    //         return ifConditionInBlock;
    //     }
    //
    //     bool ArePreviousConditionalsFalse()
    //     {
    //         return _includeTokensStack
    //             .TakeWhileWithLastInclude(i =>
    //                 !_conditionalBlockInitialWords.Contains(i.KeyWord))
    //             .All(i => !i.Flag);
    //     }
    //
    //     var hash = ConsumeNext(Hash);
    //     line = hash.Range.Start.Line;
    //     var preprocessorToken = ConsumeNext(PreprocessingToken);
    //
    //     switch (preprocessorToken.Text)
    //     {
    //         case Directives.Include:
    //         {
    //             if (!IncludeTokens)
    //             {
    //                 // Ignore everything after #include in a disabled block
    //                 foreach (var _ in ConsumeLineAll()) {}
    //                 return [];
    //             }
    //
    //             var filePath = ConsumeNext(HeaderName).Text;
    //             var tokensList = new List<IToken<CPreprocessorTokenType>>();
    //             var includeFilePath = LookUpIncludeFile(filePath);
    //             if (!IncludeContext.ShouldIncludeFile(includeFilePath))
    //             {
    //                 return [];
    //             }
    //
    //             if (!File.Exists(includeFilePath))
    //             {
    //                 Console.Error.WriteLine($"Cannot find path to {filePath} during parsing {CompilationUnitPath}");
    //             }
    //
    //             using var reader = IncludeContext.OpenFileStream(includeFilePath);
    //             await foreach (var token in ProcessInclude(includeFilePath, reader))
    //             {
    //                 tokensList.Add(token);
    //             }
    //
    //             bool hasRemaining;
    //             while ((hasRemaining = enumerator.MoveNext())
    //                    && enumerator.Current is { Kind: WhiteSpace or Comment })
    //             {
    //                 // eat remaining whitespace and comments
    //             }
    //
    //             if (hasRemaining && enumerator.Current is var t and not { Kind: WhiteSpace })
    //                 throw new PreprocessorException($"Invalid token after include path: {t.Kind} {t.Text}");
    //
    //             return tokensList;
    //         }
    //         case Directives.Error:
    //         {
    //             var errorText = new StringBuilder();
    //             while (enumerator.MoveNext())
    //             {
    //                 errorText.Append(enumerator.Current.Text);
    //             }
    //
    //             if (IncludeTokens)
    //                 throw new PreprocessorException($"Error: {errorText.ToString().Trim()}");
    //
    //             return [];
    //         }
    //         case Directives.Define:
    //         {
    //             if (!IncludeTokens) return [];
    //
    //             var expressionTokens = ConsumeLineAll();
    //             var (macroDefinition, replacement) = EvaluateMacroDefinition(expressionTokens.ToList());
    //             MacroContext.DefineMacro(macroDefinition.Name, macroDefinition, replacement);
    //
    //             return [];
    //         }
    //         case Directives.Undef:
    //         {
    //             if (!IncludeTokens) return [];
    //
    //             var expressionTokens = ConsumeLineAll();
    //             var (macroDefinition, replacement) = EvaluateMacroDefinition(expressionTokens.ToList());
    //             MacroContext.UndefineMacro(macroDefinition.Name);
    //
    //             return [];
    //         }
    //         case Directives.IfDef:
    //         {
    //             if (UpperConditionInStackIsFalse)
    //             {
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.IfDef, false, false));
    //                 return [];
    //             }
    //
    //             var identifier = ConsumeNext(PreprocessingToken).Text;
    //             var includeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
    //             _includeTokensStack.Push(new ConditionalElementResult(Directives.IfDef, includeTokens, true));
    //             return [];
    //         }
    //         case Directives.If:
    //         {
    //             if (UpperConditionInStackIsFalse)
    //             {
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.If, false, false));
    //                 return [];
    //             }
    //
    //             var expressionTokens = ConsumeLine();
    //             var includeTokens = EvaluateExpression(expressionTokens.ToList());
    //             _includeTokensStack.Push(new ConditionalElementResult(Directives.If, includeTokens, true));
    //             return [];
    //         }
    //         case Directives.IfnDef:
    //         {
    //             if (UpperConditionInStackIsFalse)
    //             {
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.IfnDef, false, false));
    //                 return [];
    //             }
    //
    //             var identifier = ConsumeNext(PreprocessingToken).Text;
    //             var doNotIncludeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
    //             _includeTokensStack.Push(new ConditionalElementResult(
    //                 Directives.IfnDef,
    //                 !doNotIncludeTokens,
    //                 true));
    //             return [];
    //         }
    //         case Directives.ElifDef:
    //         {
    //             var ifConditionInBlock = GetIfInBlockForElif();
    //             if (ifConditionInBlock.UpperFlag is not null && (bool)!ifConditionInBlock.UpperFlag)
    //             {
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.ElifDef, false, null));
    //                 return [];
    //             }
    //
    //             if (ArePreviousConditionalsFalse())
    //             {
    //                 var identifier = ConsumeNext(PreprocessingToken).Text;
    //                 var includeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
    //                 _includeTokensStack.Push(new ConditionalElementResult(
    //                     Directives.ElifDef,
    //                     includeTokens,
    //                     null));
    //                 return [];
    //             }
    //
    //             _includeTokensStack.Push(new ConditionalElementResult(Directives.ElifDef, false, null));
    //             return [];
    //         }
    //         case Directives.ElifNDef:
    //         {
    //             var ifConditionInBlock = GetIfInBlockForElif();
    //             if (ifConditionInBlock.UpperFlag is not null && (bool)!ifConditionInBlock.UpperFlag)
    //             {
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.ElifNDef, false, null));
    //                 return [];
    //             }
    //
    //             if (ArePreviousConditionalsFalse())
    //             {
    //                 var identifier = ConsumeNext(PreprocessingToken).Text;
    //                 var includeTokens = MacroContext.TryResolveMacro(identifier, out _, out var macroReplacement);
    //                 _includeTokensStack.Push(new ConditionalElementResult(
    //                     Directives.ElifNDef,
    //                     !includeTokens,
    //                     null));
    //                 return [];
    //             }
    //
    //             _includeTokensStack.Push(new ConditionalElementResult(Directives.ElifNDef, false, null));
    //             return [];
    //         }
    //         case Directives.Elif:
    //         {
    //             var ifConditionInBlock = GetIfInBlockForElif();
    //             if (ifConditionInBlock.UpperFlag is not null && (bool)!ifConditionInBlock.UpperFlag)
    //             {
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.Elif, false, null));
    //                 return [];
    //             }
    //
    //             if (ArePreviousConditionalsFalse())
    //             {
    //                 var expressionTokens = ConsumeLine();
    //                 var includeTokens = EvaluateExpression(expressionTokens.ToList());
    //                 _includeTokensStack.Push(new ConditionalElementResult(Directives.Elif, includeTokens, null));
    //                 return [];
    //             }
    //
    //             _includeTokensStack.Push(new ConditionalElementResult(Directives.Elif, false, null));
    //             return [];
    //         }
    //         case Directives.Endif:
    //         {
    //             _includeTokensStack.PopWhileWithLastInclude(i =>
    //                 !_conditionalBlockInitialWords.Contains(i.KeyWord));
    //             return [];
    //         }
    //         case Directives.Else:
    //         {
    //             _includeTokensStack.Push(new ConditionalElementResult(
    //                 Directives.Elif,
    //                 ArePreviousConditionalsFalse(),
    //                 null));
    //             return [];
    //         }
    //         case Directives.Pragma:
    //         {
    //             var identifier = ConsumeNext(PreprocessingToken).Text;
    //             if (identifier == "once")
    //             {
    //                 IncludeContext.RegisterGuardedFileInclude(CompilationUnitPath);
    //             }
    //
    //             return [];
    //         }
    //         default:
    //             throw new WipException(
    //                 77,
    //                 $"Preprocessor directive not supported: {preprocessorToken.Kind} {preprocessorToken.Text}.");
    //     }
    // }
    //private bool EvaluateExpression(IEnumerable<IToken<CPreprocessorTokenType>> expressionTokens)
    //{
    //    var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
    //        expressionTokens.Union(new[]
    //        {
    //            new Token<CPreprocessorTokenType>(
    //                new Range(),
    //                new Location(),
    //                "",
    //                End)
    //        })).ToBuffered();
    //    var p = new CPreprocessorExpressionParser(stream);
    //    var expression = p.ParseExpression();
    //    if (expression.IsError)
    //    {
    //        throw new PreprocessorException($"Cannot parse {(expression.Error.Elements.FirstOrDefault().Key)}," +
    //                                        $" got {expression.Error.Got}");
    //    }
//
    //    var macroExpression = expression.Ok.Value.EvaluateExpression(MacroContext);
    //    Debug.Assert(stream.IsEnd || stream.Peek().Kind == End);
    //    bool includeTokens = macroExpression.AsBoolean();
    //    return includeTokens;
    //}
    // private static (MacroDefinition, List<IToken<CPreprocessorTokenType>>) EvaluateMacroDefinition(
    //     IEnumerable<IToken<CPreprocessorTokenType>> expressionTokens)
    // {
    //     var stream = new EnumerableStream<IToken<CPreprocessorTokenType>>(
    //         expressionTokens.Union(new[]
    //         {
    //             new Token<CPreprocessorTokenType>(
    //                 new Range(),
    //                 new Location(),
    //                 "",
    //                 End)
    //         })).ToBuffered();
    //     var p = new CPreprocessorMacroDefinitionParser(stream);
    //     var macroDefinition = p.ParseMacro();
    //     var macroReplacement = new List<IToken<CPreprocessorTokenType>>();
    //     while (!stream.IsEnd)
    //     {
    //         var token = stream.Consume();
    //         if (token is not { Kind: End })
    //         {
    //             macroReplacement.Add(token);
    //         }
    //     }
    //
    //     if (macroDefinition.IsError)
    //     {
    //         var expected = string.Join(",",
    //             macroDefinition
    //                 .Error
    //                 .Elements
    //                 .Values
    //                 .Select(_ => $"{_.Context},{string.Join(",", _.Expected)}"));
    //         throw new PreprocessorException(
    //             $"Cannot parse macro definition. Expected: {expected} got: {macroDefinition.Error.Got}");
    //     }
    //
    //     return (macroDefinition.Ok.Value, macroReplacement);
    // }

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
}

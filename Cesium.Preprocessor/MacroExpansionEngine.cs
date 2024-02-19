using System.Globalization;
using System.Text;
using Cesium.Core;
using Cesium.Core.Warnings;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

public class MacroExpansionEngine(IWarningProcessor warningProcessor, IMacroContext macroContext)
{
    public IEnumerable<IToken<CPreprocessorTokenType>> ExpandMacros(IEnumerable<IToken<CPreprocessorTokenType>> tokens)
    {
        using var lexer = new TransactionalLexer(tokens, warningProcessor);
        while (!lexer.IsEnd)
        {
            var token = lexer.Consume();
            if (token.Kind == CPreprocessorTokenType.PreprocessingToken)
            {
                var macroName = token.Text;
                if (!macroContext.TryResolveMacro(macroName, out var parameters, out var replacement))
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
                    CPreprocessor.RaisePreprocessorParseError(arguments.Error);

                foreach (var replaced in ExpandMacros(SubstituteMacroArguments(token, arguments.Ok, replacement)))
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
                           ?? new SourceLocationInfo("<unknown>", null, null);
            transaction.End(ParseResult.Error(",", null, location, "macro arguments"));
            return null;
        }

        if (Consume() is var leftBrace and not { Kind: CPreprocessorTokenType. LeftParen })
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
            while (Peek() is not { Kind: CPreprocessorTokenType.RightParen })
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

        if (Consume() is var token and not { Kind: CPreprocessorTokenType.RightParen })
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
            } while (currentToken is { Kind: CPreprocessorTokenType.WhiteSpace or CPreprocessorTokenType.Comment });

            return currentToken;
        }

        IToken<CPreprocessorTokenType> Peek()
        {
            IToken<CPreprocessorTokenType> currentToken;
            var index = 0;
            do
            {
                currentToken = lexer.Peek(index++);
            } while (currentToken is { Kind: CPreprocessorTokenType.WhiteSpace or CPreprocessorTokenType.Comment });

            return currentToken;
        }
    }

    /// <remarks>ISO C Standard, section 6.10.4.1 Argument substitution.</remarks>
    private IEnumerable<IToken<CPreprocessorTokenType>> SubstituteMacroArguments(
        IToken macroNameToken,
        MacroArguments arguments,
        IEnumerable<IToken<CPreprocessorTokenType>> replacement)
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
                    CPreprocessorTokenType.PreprocessingToken);
                yield break;
            case "__LINE__":
            {
                var line = macroNameToken.Location.Range.Start.Line + 1;
                yield return new Token<CPreprocessorTokenType>(
                    macroNameToken.Range,
                    macroNameToken.Location,
                    line.ToString(CultureInfo.InvariantCulture),
                    CPreprocessorTokenType.PreprocessingToken);
                yield break;
            }
        }

        using var lexer = new TransactionalLexer(replacement, warningProcessor);
        var spaceBuffer = new List<IToken<CPreprocessorTokenType>>();
        IEnumerable<IToken<CPreprocessorTokenType>> ClearSpaceBuffer()
        {
            foreach (var space in spaceBuffer)
            {
                yield return space;
            }

            spaceBuffer.Clear();
        }

        while (!lexer.IsEnd)
        {
            var token = lexer.Consume();
            switch (token)
            {
                case { Kind: CPreprocessorTokenType.WhiteSpace }:
                    spaceBuffer.Add(token);
                    break;
                case { Text: "#" } when PeekSignificant() is { Kind: CPreprocessorTokenType.PreprocessingToken }:
                {
                    var next = ConsumeSignificant();
                    var sequence = ExpandMacros(ProcessTokenNoHash(next));

                    foreach (var space in ClearSpaceBuffer())
                    {
                        yield return space;
                    }

                    yield return new Token<CPreprocessorTokenType>(
                        next.Range,
                        next.Location,
                        Stringify(sequence),
                        CPreprocessorTokenType.PreprocessingToken);
                    break;
                }
                case { Text: "##" }:
                {
                    // Drop buffered spaces.
                    spaceBuffer.Clear();

                    if (PeekSignificant() is null)
                    {
                        throw new PreprocessorException(
                            token.Location,
                            "## cannot appear at the end of a macro replacement list.");
                    }

                    var next = ConsumeSignificant();
                    var sequence = ExpandMacros(ProcessTokenNoHash(next));

                    // TODO[#542]: Figure out what to do if the sequence is more than one item.
                    yield return new Token<CPreprocessorTokenType>(
                        next.Range,
                        next.Location,
                        sequence.Single().Text,
                        CPreprocessorTokenType.PreprocessingToken);
                    break;
                }
                default:
                {
                    foreach (var space in ClearSpaceBuffer())
                    {
                        yield return space;
                    }

                    var sequence = ExpandMacros(ProcessTokenNoHash(token));
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
                if (currentToken is not
                    {
                        Kind: CPreprocessorTokenType.WhiteSpace
                            or CPreprocessorTokenType.Comment
                            or CPreprocessorTokenType.NewLine
                    })
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
            } while (currentToken is
                     {
                         Kind: CPreprocessorTokenType.WhiteSpace
                            or CPreprocessorTokenType.Comment
                            or CPreprocessorTokenType.NewLine
                     });

            return currentToken;
        }

        IEnumerable<IToken<CPreprocessorTokenType>> ProcessTokenNoHash(IToken<CPreprocessorTokenType> token)
        {
            switch (token)
            {
                case { Kind: CPreprocessorTokenType.PreprocessingToken, Text: var text }
                    when arguments.Named.TryGetValue(text, out var argument):
                {
                    // macro argument substitution
                    foreach (var argumentToken in argument)
                    {
                        yield return argumentToken;
                    }

                    break;
                }
                // TODO[#541]: __VA_OPT__, see also rules for __VA_ARGS__ regarding the nested expansion.
                case { Kind: CPreprocessorTokenType.PreprocessingToken, Text: "__VA_ARGS__" }:
                {
                    var isFirst = true;
                    foreach (var varArg in arguments.VarArg)
                    {
                        // Comma before each but the first.
                        if (isFirst) isFirst = false;
                        else
                        {
                            yield return new Token<CPreprocessorTokenType>(
                                new Yoakke.SynKit.Text.Range(),
                                new Location(),
                                ",",
                                CPreprocessorTokenType.Separator);
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
                    if (token is {
                            Kind: CPreprocessorTokenType.WhiteSpace
                                or CPreprocessorTokenType.Comment
                                or CPreprocessorTokenType.NewLine
                        })
                    {
                        if (spaceEater) continue;

                        spaceEater = true;
                        var spaceToken = token.Text == " " ? token : new Token<CPreprocessorTokenType>(
                            token.Range,
                            token.Location,
                            " ",
                            CPreprocessorTokenType.WhiteSpace);
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

    private ParseResult<List<IToken<CPreprocessorTokenType>>> ParseArgument(TransactionalLexer lexer)
    {
        using var transaction = lexer.BeginTransaction();

        if (lexer.IsEnd)
            return ParseResult.Error("argument", "end of stream", 0, "macro argument");

        SourceLocationInfo argumentStartLocation = lexer.Peek().Location;
        var argument = new List<IToken<CPreprocessorTokenType>>();
        while (!lexer.IsEnd && lexer.Peek() is not ({ Kind: CPreprocessorTokenType.RightParen } or { Text: "," }))
        {
            var token = lexer.Consume();
            argument.Add(token);
            if (token is { Kind: CPreprocessorTokenType.LeftParen })
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
            while (!lexer.IsEnd && lexer.Peek() is not { Kind: CPreprocessorTokenType.RightParen })
            {
                var token = lexer.Consume();
                argument.Add(token);
                if (token is { Kind: CPreprocessorTokenType.LeftParen })
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
            tokens.SkipWhile(t => t is
            {
                Kind: CPreprocessorTokenType.WhiteSpace
                    or CPreprocessorTokenType.Comment
                    or CPreprocessorTokenType.NewLine
            });
    }

    private record MacroArguments(
        Dictionary<string, List<IToken<CPreprocessorTokenType>>> Named,
        List<List<IToken<CPreprocessorTokenType>>> VarArg
    );
}

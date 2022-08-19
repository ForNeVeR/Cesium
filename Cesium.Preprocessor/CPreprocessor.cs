using System.Globalization;
using System.Text;
using Cesium.Core;
using Yoakke.Streams;
using Yoakke.SynKit.Lexer;
using static Cesium.Preprocessor.CPreprocessorTokenType;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Preprocessor;

public record CPreprocessor(ILexer<IToken<CPreprocessorTokenType>> Lexer, IIncludeContext IncludeContext, IMacroContext MacroContext)
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
                case LeftParent:
                case RightParent:
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
                        if (MacroContext.TryResolveMacro(token.Text, out var tokenReplacement))
                        {
                            yield return new Token<CPreprocessorTokenType>(token.Range, tokenReplacement, token.Kind);
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

        var hash = ConsumeNext(Hash);
        line = hash.Range.Start.Line;

        var keyword = ConsumeNext(PreprocessingToken);
        switch (keyword.Text)
        {
            case "include":
            {
                var filePath = ConsumeNext(HeaderName).Text;
                using var reader = await LookUpIncludeFile(filePath);
                var tokensList = new List<IToken<CPreprocessorTokenType>>();
                await foreach (var token in ProcessInclude(reader))
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
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool moved;
                while ((moved = enumerator.MoveNext()) && enumerator.Current is { Kind: WhiteSpace })
                {
                    // Skip any whitespace in between tokens.
                }

                StringBuilder? sb = null;
                if (enumerator.Current is not { Kind: NewLine })
                {
                    moved = false;
                    sb = new StringBuilder(enumerator.Current.Text);
                    while ((moved = enumerator.MoveNext()) && enumerator.Current is not { Kind: NewLine })
                    {
                        sb.Append(enumerator.Current.Text);
                    }
                }

                MacroContext.DefineMacro(identifier, sb?.ToString());
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "ifdef":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool includeTokens = MacroContext.TryResolveMacro(identifier, out var macroReplacement);
                IncludeTokens = includeTokens;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "ifndef":
            {
                var identifier = ConsumeNext(PreprocessingToken).Text;
                bool donotIncludeTokens = MacroContext.TryResolveMacro(identifier, out var macroReplacement);
                IncludeTokens = !donotIncludeTokens;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            case "endif":
            {
                IncludeTokens = true;
                return Array.Empty<IToken<CPreprocessorTokenType>>();
            }
            default:
                throw new WipException(
                    77,
                    $"Preprocessor directive not supported: {keyword.Kind} {keyword.Text}.");
        }
    }

    private ValueTask<TextReader> LookUpIncludeFile(string filePath) => filePath[0] switch
    {
        '<' => IncludeContext.LookUpAngleBracedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        '"' => IncludeContext.LookUpQuotedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        _ => throw new Exception($"Unknown kind of include file path: {filePath}.")
    };

    private async IAsyncEnumerable<IToken<CPreprocessorTokenType>> ProcessInclude(TextReader fileReader)
    {
        var lexer = new CPreprocessorLexer(fileReader);
        var subProcessor = new CPreprocessor(lexer, IncludeContext, MacroContext);
        await foreach (var item in subProcessor.GetPreprocessingResults())
        {
            yield return item;
        }

        yield return new Token<CPreprocessorTokenType>(new Range(), "\n", NewLine);
    }
}

using System.Globalization;
using System.Text;
using Yoakke.Lexer;
using Yoakke.Streams;
using static Cesium.Preprocessor.CPreprocessorTokenType;
using Range = Yoakke.Text.Range;

namespace Cesium.Preprocessor;

public record CPreprocessor(ILexer<IToken<CPreprocessorTokenType>> Lexer, IIncludeContext IncludeContext)
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
                    yield return token;
                    break;

                case NewLine:
                    newLine = true;
                    yield return token;
                    break;

                case Hash:
                    if (newLine)
                    {
                        // TODO: Recursive processing
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
                case PreprocessingToken:
                    newLine = false;
                    yield return token;
                    break;

                default:
                    throw new NotSupportedException($"Illegal token {token.Kind} {token.Text}.");
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
                throw new NotSupportedException(
                    "Preprocessing directive too short at line " +
                    $"{line?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}.");

            var token = enumerator.Current;
            if (allowedTypes.Contains(token.Kind)) return enumerator.Current;

            var expectedTypeString = string.Join(" or ", allowedTypes);
            throw new NotSupportedException(
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
                var tokens = ProcessInclude(reader);

                bool hasRemaining;
                while ((hasRemaining = enumerator.MoveNext())
                       && enumerator.Current is { Kind: WhiteSpace })
                {
                    // eat remaining whitespace
                }

                if (hasRemaining && enumerator.Current is var t and not { Kind: WhiteSpace })
                    throw new NotSupportedException($"Invalid token after include path: {t.Kind} {t.Text}");

                return tokens.ToList();
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
            default:
                throw new NotSupportedException(
                    $"Preprocessor directive not supported: {keyword.Kind} {keyword.Text}.");
        }
    }

    private ValueTask<TextReader> LookUpIncludeFile(string filePath) => filePath[0] switch
    {
        '<' => IncludeContext.LookUpAngleBracedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        '"' => IncludeContext.LookUpQuotedIncludeFile(filePath.Substring(1, filePath.Length - 2)),
        _ => throw new Exception($"Unknown kind of include file path: {filePath}.")
    };

    private IEnumerable<IToken<CPreprocessorTokenType>> ProcessInclude(TextReader fileReader)
    {
        var lexer = new CPreprocessorLexer(fileReader);
        var stream = lexer.ToStream();
        while (!stream.IsEnd)
            yield return stream.Consume();

        yield return new Token<CPreprocessorTokenType>(new Range(), "\n", NewLine);
    }
}

using System.Text;
using Yoakke.Lexer;
using Yoakke.Streams;

namespace Cesium.Preprocessor;

public record CPreprocessor(ILexer<IToken<CPreprocessorTokenType>> Lexer)
{
    public string ProcessSource()
    {
        var buffer = new StringBuilder();
        foreach (var t in GetPreprocessingResults())
        {
            buffer.Append(t.Text);
        }

        return buffer.ToString();
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> GetPreprocessingResults()
    {
        var newLine = true;

        var stream = Lexer.ToStream();
        while (!stream.IsEnd)
        {
            var token = stream.Consume();
            switch (token.Kind)
            {
                case CPreprocessorTokenType.End:
                    yield break;

                case CPreprocessorTokenType.WhiteSpace:
                case CPreprocessorTokenType.Comment:
                    yield return token;
                    break;

                case CPreprocessorTokenType.NewLine:
                    newLine = true;
                    yield return token;
                    break;

                case CPreprocessorTokenType.Hash:
                    if (newLine)
                    {
                        // TODO: Recursive processing
                        foreach (var t in ProcessDirective(ReadDirectiveLine(token, stream)))
                            yield return t;
                    }

                    newLine = false;
                    break;

                case CPreprocessorTokenType.Error:
                case CPreprocessorTokenType.DoubleHash:
                case CPreprocessorTokenType.HeaderName:
                case CPreprocessorTokenType.PreprocessingToken:
                    newLine = false;
                    yield return token;
                    break;

                default:
                    throw new NotSupportedException($"Illegal token {token.Kind} {token.Text}.");
            }
        }
    }

    private IEnumerable<IToken<CPreprocessorTokenType>> ProcessDirective(
        IEnumerable<IToken<CPreprocessorTokenType>> directiveTokens)
    {
        return directiveTokens;
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
                case CPreprocessorTokenType.NewLine:
                case CPreprocessorTokenType.End:
                    yield return token;
                    yield break;
                default:
                    yield return token;
            }
        }
    }
}

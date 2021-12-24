using System.Text;
using Yoakke.Lexer;
using Yoakke.Streams;

namespace Cesium.Preprocessor;

public record CPreprocessor(ILexer<IToken<CPreprocessorTokenType>> Lexer)
{
    private IEnumerable<IToken<CPreprocessorTokenType>> GetPreprocessingResults()
    {
        var stream = Lexer.ToStream();
        while (!stream.IsEnd)
        {
            yield return stream.Consume();
        }
    }

    public string ProcessSource()
    {
        var buffer = new StringBuilder();
        foreach (var t in GetPreprocessingResults())
        {
            buffer.Append(t.Text);
        }

        return buffer.ToString();
    }
}

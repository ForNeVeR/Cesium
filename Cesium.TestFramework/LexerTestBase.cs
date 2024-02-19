using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.TestFramework;

public class LexerTestBase : VerifyTestBase
{
    protected static IEnumerable<CToken> GetTokens(string source)
    {
        var lexer = new CLexer(source);
        var stream = lexer.ToStream();
        while (stream.TryConsume(out var token) && token.Kind != CTokenType.End)
        {
            yield return token;
        }
    }
}

using Yoakke.C.Syntax;
using Yoakke.Lexer;

namespace Cesium.Test.Framework;

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

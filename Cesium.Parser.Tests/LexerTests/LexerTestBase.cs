using Cesium.Test.Framework;
using Yoakke.C.Syntax;
using Yoakke.Lexer;

namespace Cesium.Parser.Tests.LexerTests;

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

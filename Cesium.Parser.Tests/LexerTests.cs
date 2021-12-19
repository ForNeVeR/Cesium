using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yoakke.C.Syntax;
using Yoakke.Lexer;

namespace Cesium.Parser.Tests;

public class LexerTests
{
    private static IEnumerable<CToken> GetTokens(string source)
    {
        var lexer = new CLexer(source);
        var stream = lexer.ToStream();
        while (stream.TryConsume(out var token) && token.Kind != CTokenType.End)
        {
            yield return token;
        }
    }

    [Fact]
    public void SimpleTest()
    {
        const string source = "int main() {}";
        var tokens = GetTokens(source).Select(t => $"{t.Kind}: {t.Text}");
        Assert.Equal(new[]
        {
            "KeywordInt: int",
            "Identifier: main",
            "OpenParen: (",
            "CloseParen: )",
            "OpenBrace: {",
            "CloseBrace: }"
        }, tokens);
    }
}

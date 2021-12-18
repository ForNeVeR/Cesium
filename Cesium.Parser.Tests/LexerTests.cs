using System.Collections.Generic;
using System.Linq;
using Cesium.Lexer;
using Xunit;
using Yoakke.Lexer;

namespace Cesium.Parser.Tests;

public class LexerTests
{
    private static IEnumerable<Token<TokenType>> GetTokens(string source)
    {
        var lexer = new CLexer(source);
        var stream = lexer.ToStream();
        while (stream.TryConsume(out var token) && token.Kind != TokenType.End)
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
            "Keyword: int",
            "Identifier: main",
            "Punctuator: (",
            "Punctuator: )",
            "Punctuator: {",
            "Punctuator: }"
        }, tokens);
    }
}

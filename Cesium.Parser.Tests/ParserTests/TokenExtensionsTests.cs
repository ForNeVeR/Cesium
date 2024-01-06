using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Parser.Tests.ParserTests;

public class TokenExtensionsTests
{
    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"\\n\"", "\n")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\\\x10\"", "\\x10")]
    [InlineData("\"\\x20\"", " ")]
    [InlineData("\"\\\\00\"", "\\00")]
    [InlineData("\"\\00\"", "\0")]
    [InlineData("\"\\0\"", "\0")]
    [InlineData("\"\\x\"", "\\x")]
    public void Test(string tokenText, string expected)
    {
        var token = new Token<CTokenType>(new Yoakke.SynKit.Text.Range(), new Yoakke.SynKit.Text.Location(), tokenText, CTokenType.StringLiteral);

        var actual = token.UnwrapStringLiteral();

        Assert.Equal(expected, actual);
    }
}

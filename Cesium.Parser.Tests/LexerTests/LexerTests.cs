using Yoakke.C.Syntax;
using Yoakke.Lexer;

namespace Cesium.Parser.Tests.LexerTests;

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

    [Fact]
    public void ParametersTest()
    {
        const string source = "int main(int argc, char  *argv [ ]) {}";
        var tokens = GetTokens(source).Select(t => $"{t.Kind}: {t.Text}");
        Assert.Equal(new[]
        {
            "KeywordInt: int",
            "Identifier: main",
            "OpenParen: (",
            "KeywordInt: int",
            "Identifier: argc",
            "Comma: ,",
            "KeywordChar: char",
            "Multiply: *",
            "Identifier: argv",
            "OpenBracket: [",
            "CloseBracket: ]",
            "CloseParen: )",
            "OpenBrace: {",
            "CloseBrace: }"
        }, tokens);
    }

    [Theory]
    [MemberData(nameof(InvalidIdentifiers))]
    public void IncorrectIdentifierTest(string source)
    {
        var tokens = string.Join("||", GetTokens(source).Select(t => $"{t.Kind}: {t.Text}"));
        Assert.NotEqual("Identifier: " + source, tokens);
    }

    [Theory]
    [MemberData(nameof(ValidIdentifiers))]
    public void ValidIdentifierTest(string source)
    {
        var tokens = string.Join("||", GetTokens(source).Select(t => $"{t.Kind}: {t.Text}"));
        Assert.Equal("Identifier: " + source, tokens);
    }

    public static IEnumerable<object[]> InvalidIdentifiers()
    {
        yield return new object[] { "main$" };
        yield return new object[] { "main#" };
        yield return new object[] { "main+" };
        yield return new object[] { "main&" };
        yield return new object[] { "9G" };

        // TODO: This is valid tests which cannot be expressed in Yoakke
        //yield return new object[] { "extremely_long_identifier_of_more_then_31_character" };
    }

    public static IEnumerable<object[]> ValidIdentifiers()
    {
        yield return new object[] { "main_" };
        yield return new object[] { "MAIN" };
        yield return new object[] { "MAIN20002" };

        // This is extension to C standard.
        yield return new object[] { "Ä" };
        yield return new object[] { "\u0410" }; // Cyrillic A
        yield return new object[] { "\u0100" }; // Latin Ā
    }

}

using Cesium.TestFramework;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.Parser.Tests.LexerTests;

public class IdentifierTests : LexerTestBase
{
    [Theory]
    [MemberData(nameof(InvalidIdentifiers))]
    public void IncorrectIdentifierTest(string source)
    {
        var tokens = GetTokens(source).ToList();
        Assert.False(tokens.Count == 1 && tokens[0].Kind == CTokenType.Identifier && tokens[0].Text == source);
    }

    [Theory]
    [MemberData(nameof(ValidIdentifiers))]
    public void ValidIdentifierTest(string source)
    {
        var token = GetTokens(source).Single();
        Assert.Equal(CTokenType.Identifier, token.Kind);
        Assert.Equal(source, token.Text);
    }

    public static IEnumerable<object[]> InvalidIdentifiers()
    {
        yield return new object[] { "main$" };
        yield return new object[] { "main#" };
        yield return new object[] { "main+" };
        yield return new object[] { "main&" };
        yield return new object[] { "9G" };

        // TODO[#237]: This is valid tests which cannot be expressed in Yoakke
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

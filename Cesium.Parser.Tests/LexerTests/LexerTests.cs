namespace Cesium.Parser.Tests.LexerTests;

public class LexerTests : LexerTestBase
{
    private static Task DoLexerTest(string source)
    {
        var tokens = GetTokens(source).Select(t => $"{t.Kind}: {t.Text}");
        return Verify(string.Join("\n", tokens));
    }

    [Fact]
    public Task SimpleTest() => DoLexerTest("int main() {}");

    [Fact]
    public Task ParametersTest() => DoLexerTest("int main(int argc, char  *argv [ ]) {}");
}

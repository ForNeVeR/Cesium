using Cesium.TestFramework;

namespace Cesium.Parser.Tests.LexerTests;

public class LexerTests : LexerTestBase
{
    private static Task DoLexerTest(string source)
    {
        var tokens = GetTokens(source).Select(t => $"{t.Kind}: {t.Text}");
        return Verify(string.Join("\n", tokens), GetSettings());
    }

    [Fact]
    public Task SimpleTest() => DoLexerTest("int main() {}");

    [Fact]
    public Task ParametersTest() => DoLexerTest("int main(int argc, char  *argv [ ]) {}");

    [Fact]
    public Task CliImportTest() => DoLexerTest(@"__cli_import(""System.Runtime.InteropServices.Marshal::AllocHGlobal"")
void *malloc(size_t);");
}

using Cesium.Preprocessor;
using Cesium.TestFramework;
using Yoakke.SynKit.Lexer;

namespace Cesium.Parser.Tests.LexerTests;

public class PreprocessorLexerTests : VerifyTestBase
{
    private static IEnumerable<IToken<CPreprocessorTokenType>> GetTokens(string source)
    {
        var lexer = new CPreprocessorLexer(source);
        var stream = lexer.ToStream();
        while (stream.TryConsume(out var token) && token.Kind != CPreprocessorTokenType.End)
        {
            yield return token;
        }
    }

    private static Task DoTest(string source)
    {
        var tokens = GetTokens(source).Select(t => $"{t.Kind}: \"{t.Text.ReplaceLineEndings(@"\n")}\"");
        return Verify(string.Join("\n", tokens), GetSettings());
    }

    [Fact]
    public Task SimpleSource() => DoTest(@"int main() {}");

    [Fact]
    public Task Include() => DoTest(@"#include ""foo.h""
int main() {}");

    [Fact]
    public Task Pragma() => DoTest(@"#pragma include ""foo.h""
int main() {}");
}

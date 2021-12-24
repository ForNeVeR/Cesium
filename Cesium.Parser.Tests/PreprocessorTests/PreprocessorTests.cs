using Cesium.Preprocessor;
using Cesium.Test.Framework;

namespace Cesium.Parser.Tests.PreprocessorTests;

public class PreprocessorTests : VerifyTestBase
{
    private static Task DoTest(string source)
    {
        var lexer = new CPreprocessorLexer(source);
        var preprocessor = new CPreprocessor(lexer);
        return Verify(preprocessor.ProcessSource());
    }

    [Fact]
    public Task IdentityTest() => DoTest(@"int main(void)
{
    return 2 + 2;
}");
}

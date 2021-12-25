using Cesium.Preprocessor;
using Cesium.Test.Framework;

namespace Cesium.Parser.Tests.PreprocessorTests;

public class PreprocessorTests : VerifyTestBase
{
    private static async Task DoTest(string source, Dictionary<string, string>? standardHeaders = null)
    {
        var lexer = new CPreprocessorLexer(source);
        var includeContext = new IncludeContextMock(standardHeaders ?? new Dictionary<string, string>());
        var preprocessor = new CPreprocessor(lexer, includeContext);
        var result = await preprocessor.ProcessSource();
        await Verify(result);
    }

    [Fact]
    public Task IdentityTest() => DoTest(@"int main(void)
{
    return 2 + 2;
}");

    [Fact]
    public Task Include() => DoTest(@"#include <foo.h>
int test()
{
    #include <bar.h>
}", new() { ["foo.h"] = "void foo() {}", ["bar.h"] = "int bar = 0;" });
}

using Cesium.TestFramework;

namespace Cesium.CodeGen.Tests;

public class CodeGenPInvokeTests : CodeGenTestBase
{
    private const string _mainMockedFilePath = @"c:\a\b\c.c";

    private static async Task DoTest(string source)
    {
        var processed = await PreprocessorUtil.DoPreprocess(_mainMockedFilePath, source);
        var assembly = GenerateAssembly(default, processed);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        await VerifyMethods(moduleType);
    }

    [Fact]
    public Task SinglePinvokePragma() => DoTest(@"
#pragma pinvoke(""mydll.dll"")
int not_pinvoke(void);
int foo_bar(int*);

int main() {
    return foo_bar(0);
}

int not_pinvoke(void) { return 1; }
");

    [Fact] // win_puts -> pinvokeimpl(msvcrt, puts) int win_puts();
    public Task PInvokePrefixPragma() => DoTest(@"
#pragma pinvoke(""msvcrt"", win_)
int win_puts(const char*);
");
}

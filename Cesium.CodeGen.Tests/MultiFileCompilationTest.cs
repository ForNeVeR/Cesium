using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class MultiFileCompilationTest : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(params string[] sources)
    {
        var assembly = GenerateAssembly(default, sources);
        return VerifyTypes(assembly);
    }

    [Fact]
    public Task MultiFileProgram() => DoTest("int foo() { return 0; }", "int main() {}");

    [Fact]
    public Task ExternalLinkage() => DoTest("int foo(void) { return 0; }", @"int foo(void);
int main(void) { return foo(); }");

    [Fact]
    public Task ExternalLinkage2() => DoTest(@"int foo(void);
int main(void) { return foo(); }", "int foo(void) { return 0; }");

    [Fact]
    public Task ExternalLinkage3() => DoTest(@"extern int foo(void);
int main(void) { return foo(); }", "int foo(void) { return 0; }");
}

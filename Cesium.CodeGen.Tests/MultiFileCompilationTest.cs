namespace Cesium.CodeGen.Tests;

public class MultiFileCompilationTest : CodeGenTestBase
{
    private static Task DoTest(params string[] sources)
    {
        var assembly = GenerateAssembly(default, sources);
        return VerifyTypes(assembly);
    }

    [Fact]
    public Task MultiFileProgram() => DoTest("int foo() { return 0; }", "int main() {}");
}

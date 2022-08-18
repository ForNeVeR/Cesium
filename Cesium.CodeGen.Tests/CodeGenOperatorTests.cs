namespace Cesium.CodeGen.Tests;

public class CodeGenOperatorTests: CodeGenTestBase
{
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    // TODO[#246] Insert implicit casts during IR building
    [Fact(Skip = "Implicit casts should be part of IR in order to restore this test.")]
    public Task AddIntToChar() => DoTest(@"int main() { int x = 1 + 'b'; }");

    [Fact]
    public Task SubtractIntFromInt() => DoTest(@"int main() { int x = 2 - 1; }");
}

namespace Cesium.CodeGen.Tests;

public class CodeGenOperatorTests: CodeGenTestBase
{
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task AddIntToChar() => DoTest(@"int main() { int x = 1 + 'b'; }");

    [Fact]
    public Task SubstractIntFromInt() => DoTest(@"int main() { int x = 2 - 1; }");
}

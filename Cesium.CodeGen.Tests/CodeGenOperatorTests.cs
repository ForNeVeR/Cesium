using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenOperatorTests: CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task AddIntToChar() => DoTest(@"int main() { int x = 1 + 'b'; }");

    [Fact]
    public Task SubtractIntFromInt() => DoTest(@"int main() { int x = 2 - 1; }");

    [Fact]
    public Task ConditionalVoid() => DoTest(@"void foo() {} int main() { 1 ? foo() : foo(); }");

    [Fact]
    public Task ConditionalInt() => DoTest(@"int main() { int x = 1 ? 2 : 3; }");

    [Fact]
    public Task ConditionalFloatAndInt() => DoTest(@"int main() { float x = 1 ? 2.0f : 3; }");

    [Fact]
    public Task CommaOperator() => DoTest(@"int main() { int x = (1, 2); }");

    [Fact]
    public Task TertiaryOperator() => DoTest(@"int main() { int x = 1 > 2 ? 1 : 2; }");

    [Fact]
    public Task TertiaryOperatorWithoutAssignment() => DoTest(@"int main() { 1 > 2 ? 1 : 2; }");
}

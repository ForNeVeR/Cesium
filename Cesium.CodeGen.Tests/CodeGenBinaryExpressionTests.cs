using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenBinaryExpressionTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EqualityForBool() => DoTest(@"int main() { _Bool x = 1; _Bool y = 1; int r = x == y; }");

    [Fact]
    public Task SumIntAndBool() => DoTest(@"int main() { _Bool x = 1; _Bool y = 1; int r = 1 + (x == y); }");
}

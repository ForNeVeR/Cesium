using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenWhileTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task SimpleWhile() => DoTest(
        @"int main()
{
    int i = 0;
    while (i < 10) ++i;
}");

    [Fact]
    public Task DoWhile() => DoTest(
        @"int main()
{
    int i = 0;
    do ++i; while (i < 10);
}");
}

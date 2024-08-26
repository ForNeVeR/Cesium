using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenIfTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task SimpleIfTest() => DoTest(@"int main()
{
    if (1)
        return 0;
}");

    [Fact]
    public Task IfImplicitElseTest() => DoTest(@"int main()
{
    if (1)
        return 0;
    return 1;
}");

    [Fact]
    public Task IfElseTest() => DoTest(@"int main()
{
    int a = 0;
    if (1)
        a = 1;
    else
        a = 2;
}");

    [Fact]
    public Task IfElseReturnTest() => DoTest(@"int main()
{
    if (1)
        return 0;
    else
        return 1;
}");

    [Fact]
    public Task IfNegationTest() => DoTest(@"int main()
{
    int a = 0;
    if (!1)
        a = 1;
}");

    [Fact]
    public Task IfElseWithNestedDeclarationTest() => DoTest(@"int main()
{
    if (1) {
        int a;
        a = 1;
    }
    else {
        int a;
        a = 2;
    }
}");
}

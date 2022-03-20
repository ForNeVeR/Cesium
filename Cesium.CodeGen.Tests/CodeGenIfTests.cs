namespace Cesium.CodeGen.Tests;

public class CodeGenIfTests : CodeGenTestBase
{
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
    if (1) 
        return 0;
    else
        return 1;
}");
}

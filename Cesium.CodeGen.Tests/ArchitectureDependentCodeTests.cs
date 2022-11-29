using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentCodeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source, string @namespace = "", string globalTypeFqn = "")
    {
        Assert.True(false, "TODO: Provide architecture for tests");
        var assembly = GenerateAssembly(default, @namespace, globalTypeFqn, source);
        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Theory]
    [InlineData()] // TODO: 64b
    [InlineData()] // TODO: 32b
    [InlineData()] // TODO: dynamic
    public Task StaticArray() => DoTest("""
int main(void)
{
    int x[300];
    x[299] = 0;
    return x[299];
}
""");
}

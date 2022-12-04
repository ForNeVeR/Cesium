using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentCodeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(TargetArchitectureSet arch, string source)
    {
        var assembly = GenerateAssembly(runtime: default, arch: arch, sources: source);
        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType, arch);
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Dynamic)]
    public Task StaticArray(TargetArchitectureSet arch) => DoTest(arch, """
int main(void)
{
    int *x[300];
    x[299] = 0;
    return x[299];
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Dynamic)]
    public Task ArchDependentStructArray(TargetArchitectureSet arch) => DoTest(arch, """
typedef struct { char *ptr; } foo;

int main(void)
{
    foo x[3];
    return 0;
}
""");
}

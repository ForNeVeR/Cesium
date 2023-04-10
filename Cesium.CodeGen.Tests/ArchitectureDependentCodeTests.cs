using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentCodeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(TargetArchitectureSet arch, string source)
    {
        var assembly = GenerateAssembly(runtime: default, arch: arch, sources: source);
        return VerifyTypes(assembly, arch);
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
    public Task StructArray(TargetArchitectureSet arch) => DoTest(arch, """
typedef struct { char *ptr; } foo;

int main(void)
{
    foo x[3];
    return 0;
}
""");

    [Theory]
    // TODO[#355]: [InlineData(TargetArchitectureSet.Bit64)]
    // TODO[#355]: [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Dynamic)]
    public Task TwoMemberStructArray(TargetArchitectureSet arch) => DoTest(arch, """
typedef struct { char *ptr; int len; } foo;

int main(void)
{
    foo x[3];
    return 0;
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    public Task PointerArrayMemberAssign(TargetArchitectureSet arch) => DoTest(arch, """
int main(void)
{
    void *x[3];
    x[2] = 0;
    x[0] = x[2];
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    public Task PointerFunctionSignature(TargetArchitectureSet arch) => DoTest(arch, """
int foo(void *x)
{
    return 0;
}
""");
}

using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentTypeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(TargetArchitectureSet arch, string source, string @namespace = "", string globalTypeFqn = "")
    {
        var assembly = GenerateAssembly(
            default,
            arch,
            @namespace: @namespace,
            globalTypeFqn: globalTypeFqn,
            sources: source);
        return VerifyTypes(assembly, arch);
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task StructWithPointerArray(TargetArchitectureSet arch) => DoTest(arch, """
typedef struct
{
    char *x[1];
} foo;
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task StructWithPointer(TargetArchitectureSet arch) => DoTest(arch, """
        typedef struct
        {
            char *x;
        } foo;
        """);

    [Fact(DisplayName = "Struct with a fixed array of a pointer type isn't supported for dynamic architecture")]
    public void StructWithPointerArrayDynamic() => DoesNotCompile("""
typedef struct
{
    char *x[1];
} foo;
""",
        "Cannot statically determine a size of type",
        arch: TargetArchitectureSet.Dynamic);
}

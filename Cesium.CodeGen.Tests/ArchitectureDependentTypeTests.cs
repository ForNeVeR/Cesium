using System.Diagnostics.CodeAnalysis;
using Cesium.TestFramework;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentTypeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(
        TargetArchitectureSet arch,
        [StringSyntax("cpp")] string source)
    {
        var assembly = GenerateAssembly(
            default,
            arch,
            @namespace: "",
            globalTypeFqn: "",
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

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task StructWithDoublePointer(TargetArchitectureSet arch) => DoTest(arch, """
        typedef struct
        {
            int **x;
        } foo;
        """);

    [Fact(DisplayName = "Struct with a fixed array of a pointer type isn't supported for dynamic architecture"),
     NoVerify]
    public void StructWithPointerArrayDynamic() => DoesNotCompile("""
typedef struct
{
    char *x[1];
} foo;
""",
        "Cannot statically determine a size of type",
        arch: TargetArchitectureSet.Dynamic);
}

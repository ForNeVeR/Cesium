using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentTypeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source, string @namespace = "", string globalTypeFqn = "")
    {
        Assert.True(false, "TODO: Provide architecture for tests");
        var assembly = GenerateAssembly(default, @namespace, globalTypeFqn, source);
        return VerifyTypes(assembly);
    }

    [Theory]
    [InlineData()] // TODO: 64b
    [InlineData()] // TODO: 32b
    public Task StructWithPointer() => DoTest("""
struct foo
{
    char *x[1];
};
""");

    [Fact(DisplayName = "Struct with a fixed array of a pointer type isn't supported for dynamic architecture")]
    // TODO: Provide the architecture.
    public void StructWithPointerDynamic() => DoesNotCompile("""
struct foo
{
    char *x[1];
};
""", "Dynamic architecture doesn't support fixed struct member of architecture-dependent size.");
}

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class ArchitectureDependentCodeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(TargetArchitectureSet arch, [StringSyntax("cpp")] string source)
    {
        var assembly = GenerateAssembly(runtime: default, arch: arch, sources: source);
        return VerifyTypes(assembly, arch);
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
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
    [InlineData(TargetArchitectureSet.Wide)]
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
    // TODO[#355] [InlineData(TargetArchitectureSet.Wide)]
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
    [InlineData(TargetArchitectureSet.Wide)]
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
    [InlineData(TargetArchitectureSet.Wide)]
    public Task PointerFunctionSignature(TargetArchitectureSet arch) => DoTest(arch, """
int foo(void *x)
{
    return 0;
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task FunctionPointerParameter(TargetArchitectureSet arch) => DoTest(arch, """
typedef void (*func)(int, int);
typedef void (*v_func)(void);

int foo(func x) { return 0; }
int v_foo(v_func x) { return 0; }
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task FunctionPointerStructMember(TargetArchitectureSet arch) => DoTest(arch, """
typedef void (*func)(int);
struct Foo
{
    func x;
};
""");
    // TODO[#487]: empty-paren-func ptr
    // TODO[#487]: vararg-func ptr

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Bit64)]
    [InlineData(TargetArchitectureSet.Bit32)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task StructWithNestedFuncPtr(TargetArchitectureSet arch) => DoTest(arch, """
typedef int (*func)(void);
typedef int (*hostFunc)(func);
typedef struct
{
    hostFunc foo;
} foo;
""");
}

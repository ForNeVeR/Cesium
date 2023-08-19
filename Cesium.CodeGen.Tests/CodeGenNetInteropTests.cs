using System.Diagnostics.CodeAnalysis;

namespace Cesium.CodeGen.Tests;

public class CodeGenNetInteropTests : CodeGenTestBase
{
    private static Task DoTest(
        TargetArchitectureSet architecture,
        [StringSyntax("csharp")] string cSharpCode,
        [StringSyntax("cpp")] string cCode)
    {
        var cSharpAssemblyPath = CompileCSharpAssembly(cSharpCode);
        var cesiumAssembly = GenerateAssembly(runtime: null, arch: architecture, sources: new[]{cCode}, referencePaths: new[] { cSharpAssemblyPath });
        return VerifyTypes(cesiumAssembly, architecture);
    }

    private static string CompileCSharpAssembly(string cSharpCode)
    {
        Assert.True(false, "TODO: Compile .NET Assembly");
        return null!;
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task PointerInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"public static class Test
{
    public static int Func(int* ptr) => 1;
}
", """
__cli_import("Test::Func")
int Func(int *ptr);

int main(void)
{
    int x = 0;
    return Func(&x);
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task CPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(CPtr<int> ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(int *ptr);

   int main(void)
   {
       int x = 0;
       return Func(&x);
   }
   """);

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task VPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(CPtr<int> ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(int *ptr);

   int main(void)
   {
       int x = 0;
       return Func(&x);
   }
   """);

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task FPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(FPtr<Func<int>> ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(int (*ptr)());

   int myFunc()
   {
       return 0;
   }

   int main(void)
   {
       return Func(&myFunc);
   }
   """);
}

using System.Diagnostics.CodeAnalysis;
using Cesium.Test.Framework;
using Xunit.Abstractions;

namespace Cesium.CodeGen.Tests;

public class CodeGenNetInteropTests : CodeGenTestBase
{
    private readonly ITestOutputHelper _output;
    public CodeGenNetInteropTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private async Task DoTest(
        TargetArchitectureSet architecture,
        [StringSyntax("csharp")] string cSharpCode,
        [StringSyntax("cpp")] string cCode)
    {
        var cSharpAssemblyPath = await CSharpCompilationUtil.CompileCSharpAssembly(
            _output,
            CSharpCompilationUtil.DefaultRuntime,
            cSharpCode);
        var cesiumAssembly = GenerateAssembly(runtime: null, arch: architecture, sources: new[]{cCode}, referencePaths: new[] { cSharpAssemblyPath });
        await VerifyTypes(cesiumAssembly, architecture);
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task PointerInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"public static unsafe class Test
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
    public Task VoidPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(VoidPtr ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(void *ptr);

   int main(void)
   {
       int x = 0;
       return Func(&x);
   }
   """);

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task FuncPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(FuncPtr<Func<int>> ptr) => 1;
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

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task FunctionPointerInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        """
public static unsafe class Test
{
    public static int Func(delegate*<int> ptr) => 1;
}
""", """
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

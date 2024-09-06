using System.Diagnostics.CodeAnalysis;
using Cesium.Compiler;
using Cesium.TestFramework;
using Xunit.Abstractions;

namespace Cesium.CodeGen.Tests;

// TODO[#488]: Make them run in parallel, as all the integration tests
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
        var (cesiumAssembly, assemblyContents) = GenerateAssembly(runtime: null, arch: architecture, sources: new[]{cCode}, referencePaths: new[] { cSharpAssemblyPath });
        await VerifyTypes(cesiumAssembly, architecture);
        await VerifyAssemblyRuns(assemblyContents.ToArray(), cSharpAssemblyPath);
    }

    private async Task VerifyAssemblyRuns(byte[] assemblyContentToRun, string referencePath)
    {
        var testDirectoryPath = Path.GetTempFileName();
        File.Delete(testDirectoryPath);
        Directory.CreateDirectory(testDirectoryPath);

        try
        {
            var assemblyPath = Path.Combine(testDirectoryPath, "EntryPoint.dll");
            var runtimeConfigPath = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");

            await File.WriteAllBytesAsync(assemblyPath, assemblyContentToRun);
            await File.WriteAllTextAsync(runtimeConfigPath, RuntimeConfig.EmitNet8());

            DeployReferenceAssembly(CSharpCompilationUtil.CesiumRuntimeLibraryPath);
            DeployReferenceAssembly(referencePath);

            await ExecUtil.RunToSuccess(_output, "dotnet", testDirectoryPath, new[] { assemblyPath });
        }
        finally
        {
            Directory.Delete(testDirectoryPath, recursive: true);
        }

        void DeployReferenceAssembly(string assemblyPath)
        {
            var targetFilePath = Path.Combine(testDirectoryPath, Path.GetFileName(assemblyPath));
            File.Copy(assemblyPath, targetFilePath);
        }
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
    return Func(&x) - 1;
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
       return Func(&x) - 1;
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
       return Func(&x) - 1;
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
   return Func(&myFunc) - 1;
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
   return Func(&myFunc) - 1;
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task TestEquivalentTypeAttribute(TargetArchitectureSet architecture) => DoTest(architecture,
@"using Cesium.Runtime;
public static unsafe class Test
{
    public static int Func(UTF8String str) => (int)str.Length;
}",
@"
__cli_import(""Test::Func"")
int Func(char*);

int main(void)
{
    return Func(""Hi"") - 2;
}");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task TestEquivalentTypeAttributeUsingInReturn(TargetArchitectureSet architecture) => DoTest(architecture,
@"using Cesium.Runtime;
public static unsafe class Test
{
    public static UTF8String Func(int __unused) => UTF8String.NullString;
}",
@"
__cli_import(""Test::Func"")
char* Func(int __unused);

int main(void)
{
    return Func(11) != 0;
}");
}

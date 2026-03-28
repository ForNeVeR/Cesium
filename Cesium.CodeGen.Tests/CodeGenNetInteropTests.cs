// SPDX-FileCopyrightText: 2023-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Cesium.Compiler;
using Cesium.TestFramework;
using TruePath;
using TruePath.SystemIo;
using Xunit.Abstractions;

namespace Cesium.CodeGen.Tests;

// TODO[#488]: Make them run in parallel, as all the integration tests
public class CodeGenNetInteropTests(ITestOutputHelper output) : CodeGenTestBase
{
    private async Task DoTestCSharpLibCApp(
        TargetArchitectureSet architecture,
        [StringSyntax("csharp")] string cSharpCode,
        [StringSyntax("cpp")] string cCode)
    {
        using CSharpCompilationUtil compilation = new();
        var cSharpAssemblyPath = await compilation.CompileCSharpAssembly(
            output,
            CSharpCompilationUtil.DefaultRuntime,
            references: [],
            cSharpCode);
        var (cesiumAssembly, assemblyContents) = GenerateAssembly(
            runtime: null,
            arch: architecture,
            sources: [cCode],
            referencePaths: [cSharpAssemblyPath]);
        await VerifyTypes(cesiumAssembly, architecture);
        await VerifyAssemblyRuns(assemblyContents.ToArray(), cSharpAssemblyPath);
    }

    private async Task DoTestCLibCSharpApp(
        TargetArchitectureSet architecture,
        [StringSyntax("cpp")] string cCode,
        [StringSyntax("csharp")] string cSharpCode)
    {
        using CSharpCompilationUtil compilation = new();
        var (cesiumAssembly, assemblyContents) = GenerateAssembly(
            runtime: TargetRuntimeDescriptor.Net60,
            arch: architecture,
            sources: [cCode],
            @namespace: "CesiumLib",
            globalTypeFqn: "CesiumLib.Global",
            referencePaths: []);
        var cesiumAssemblyFile = compilation.TempDirectory / "test.dll";
        await cesiumAssemblyFile.WriteAllBytesAsync(assemblyContents);

        var cSharpAssemblyPath = await compilation.CompileCSharpAssembly(
            output,
            CSharpCompilationUtil.DefaultRuntime,
            references: [cesiumAssemblyFile],
            cSharpCode,
            isApplication: true);

        await VerifyTypes(cesiumAssembly, architecture);
        await VerifyAssemblyRuns(cSharpAssemblyPath);
    }

    private async Task VerifyAssemblyRuns(byte[] assemblyContentToRun, AbsolutePath referencePath)
    {
        var testDirectory = Temporary.CreateTempFolder();
        try
        {
            var assemblyPath = testDirectory / "EntryPoint.dll";
            var runtimeConfigPath = Path.ChangeExtension(assemblyPath.Value, ".runtimeconfig.json");

            await File.WriteAllBytesAsync(assemblyPath.Value, assemblyContentToRun);
            await File.WriteAllTextAsync(runtimeConfigPath, RuntimeConfig.EmitNet10());

            DeployReferenceAssembly(CSharpCompilationUtil.CesiumRuntimeLibraryPath);
            DeployReferenceAssembly(referencePath);

            await ExecUtil.RunToSuccess(output, ExecUtil.DotNetHost, testDirectory, [assemblyPath.Value]);
        }
        finally
        {
            Directory.Delete(testDirectory.Value, recursive: true);
        }

        void DeployReferenceAssembly(AbsolutePath assemblyPath)
        {
            var targetFilePath = testDirectory / assemblyPath.FileName;
            File.Copy(assemblyPath.Value, targetFilePath.Value);
        }
    }

    private async Task VerifyAssemblyRuns(AbsolutePath referencePath)
    {
        await ExecUtil.RunToSuccess(
            output,
            ExecUtil.DotNetHost,
            referencePath.Parent!.Value,
            [referencePath.Value]);
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task PointerInterop(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(
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
    public Task CPtrInterop(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(
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
    public Task VoidPtrInterop(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(
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
    public Task FuncPtrInterop(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(
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
    public Task FunctionPointerInterop(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(
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
    public Task TestEquivalentTypeAttribute(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(architecture,
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
    public Task TestEquivalentTypeAttributeUsingInReturn(TargetArchitectureSet architecture) => DoTestCSharpLibCApp(architecture,
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

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public Task TestCSharpReferencingCLibrary(TargetArchitectureSet architecture) =>
        DoTestCLibCSharpApp(
            architecture,
            cCode: """
typedef struct Greeting {
    char* message;
} Greeting;

int len(Greeting* greeting) {
    int count = 0;
    char* p = greeting->message;
    while (p[count] != '\0') {
        ++count;
    }
    return count;
}
""",
            cSharpCode: """
class Program
{
    static unsafe int Main()
    {
        var message = "hello\0"U8;
        fixed (byte* p = message) {
            var greeting = new CesiumLib.Greeting { message = p };
            var len = CesiumLib.Global.len(&greeting);
            if (len != 5) return 1;
        }

        return 0;
    }
}
""");
}

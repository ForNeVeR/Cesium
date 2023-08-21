using Cesium.CodeGen;
using Xunit.Abstractions;

namespace Cesium.Test.Framework;

// TODO: Make a normal disposable class to delete the whole directory in the end of the test.
public static class CSharpCompilationUtil
{
    public static readonly TargetRuntimeDescriptor DefaultRuntime = TargetRuntimeDescriptor.Net60;
    private const string _configuration = "Debug";

    /// <summary>Semaphore that controls the amount of simultaneously running tests.</summary>
    // TODO: Should not be static, make a fixture.
    private static readonly SemaphoreSlim _testSemaphore = new(Environment.ProcessorCount);

    // TODO: Support references
    public static async Task<string> CompileCSharpAssembly(
        ITestOutputHelper output,
        TargetRuntimeDescriptor runtime,
        string cSharpSource)
    {
        if (runtime != DefaultRuntime) throw new Exception($"Runtime {runtime} not supported for test compilation.");
        await _testSemaphore.WaitAsync();
        try
        {
            var directory = Path.GetTempFileName();
            File.Delete(directory);
            Directory.CreateDirectory(directory);

            var projectName = await CreateCSharpProject(output, directory);
            await File.WriteAllTextAsync(Path.Combine(directory, projectName, "Program.cs"), cSharpSource);
            await CompileCSharpProject(output, directory, projectName);
            return Path.Combine(directory, "bin", _configuration, "net6.0", projectName + ".dll");
        }
        finally
        {
            _testSemaphore.Release();
        }
    }

    private static async Task<string> CreateCSharpProject(ITestOutputHelper output, string directory)
    {
        const string projectName = "TestProject";
        await ExecUtil.RunToSuccess(output, "dotnet", directory, new[] { "new", "console", "-o", "TestProject" });
        return projectName;
    }

    private static Task CompileCSharpProject(ITestOutputHelper output, string directory, string projectName) =>
        ExecUtil.RunToSuccess(output, "dotnet", directory, new[]
        {
            "build",
            projectName,
            "--configuration", _configuration,
        });
}

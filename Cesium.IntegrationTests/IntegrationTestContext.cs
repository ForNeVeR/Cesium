using System.Reflection;
using JetBrains.Annotations;
using NeoSmart.AsyncLock;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

[UsedImplicitly]
public class IntegrationTestContext : IAsyncDisposable
{
    public static readonly string SolutionRootPath = GetSolutionRoot();
    public const string BuildConfiguration = "Release";
    private readonly AsyncLock _lock = new();
    private bool _initialized;
    private Exception? _initializationException;

    public async Task EnsureInitialized(ITestOutputHelper output)
    {
        using (await _lock.LockAsync())
        {
            if (_initialized)
            {
                if (_initializationException != null) throw _initializationException;
                return;
            }

            try
            {
                await BuildRuntime(output);
                await BuildCompiler(output);
            }
            catch (Exception ex)
            {
                _initializationException = ex;
                throw;
            }
            finally
            {
                _initialized = true;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ExecUtil.RunToSuccess(null, "dotnet", SolutionRootPath, new[]
        {
            "build-server",
            "shutdown"
        });
    }

    private static string GetSolutionRoot()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var currentDirectory = assemblyDirectory;
        while (currentDirectory != null)
        {
            if (File.Exists(Path.Combine(currentDirectory, "Cesium.sln")))
                return currentDirectory;

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        throw new Exception($"Could not find the solution directory going up from directory \"{assemblyDirectory}\".");
    }

    private async Task BuildRuntime(ITestOutputHelper output)
    {
        var runtimeProjectFile = Path.Combine(SolutionRootPath, "Cesium.Runtime/Cesium.Runtime.csproj");
        await BuildDotNetProject(output, runtimeProjectFile);
    }

    private async Task BuildCompiler(ITestOutputHelper output)
    {
        var compilerProjectFile = Path.Combine(SolutionRootPath, "Cesium.Compiler/Cesium.Compiler.csproj");
        await BuildDotNetProject(output, compilerProjectFile);
    }

    private Task BuildDotNetProject(ITestOutputHelper output, string projectFilePath) =>
        ExecUtil.RunToSuccess(output, "dotnet", Path.GetDirectoryName(projectFilePath)!, new[]
        {
            "build",
            "--configuration", BuildConfiguration,
            projectFilePath
        });
}

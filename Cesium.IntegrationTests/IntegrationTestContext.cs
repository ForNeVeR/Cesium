using System.Reflection;
using Cesium.Test.Framework;
using JetBrains.Annotations;
using NeoSmart.AsyncLock;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

[UsedImplicitly]
public class IntegrationTestContext : IAsyncDisposable
{
    public static readonly string SolutionRootPath = GetSolutionRoot();
    public const string BuildConfiguration = "Release";

    /// <summary>Semaphore that controls the amount of simultaneously running tests.</summary>
    private readonly SemaphoreSlim _testSemaphore = new(Environment.ProcessorCount);

    private readonly AsyncLock _lock = new();
    private bool _initialized;
    private Exception? _initializationException;

    public string? VisualStudioPath { get; private set; }

    public async Task WrapTestBody(Func<Task> testBody)
    {
        await _testSemaphore.WaitAsync();
        try
        {
            await testBody();
        }
        finally
        {
            _testSemaphore.Release();
        }
    }

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
                await InitializeOnce(output);
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

    private async Task InitializeOnce(ITestOutputHelper output)
    {
        if (OperatingSystem.IsWindows())
        {
            VisualStudioPath = await WindowsEnvUtil.FindVCCompilerInstallationFolder(output);
        }

        await BuildRuntime(output);
        await BuildCompiler(output);
    }

    public async ValueTask DisposeAsync()
    {
        await DotNetCliHelper.ShutdownBuildServer();
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

    private static async Task BuildRuntime(ITestOutputHelper output)
    {
        var runtimeProjectFile = Path.Combine(SolutionRootPath, "Cesium.Runtime/Cesium.Runtime.csproj");
        await DotNetCliHelper.BuildDotNetProject(output, BuildConfiguration, runtimeProjectFile);
    }

    private static async Task BuildCompiler(ITestOutputHelper output)
    {
        var compilerProjectFile = Path.Combine(SolutionRootPath, "Cesium.Compiler/Cesium.Compiler.csproj");
        await DotNetCliHelper.BuildDotNetProject(output, BuildConfiguration, compilerProjectFile);
    }
}

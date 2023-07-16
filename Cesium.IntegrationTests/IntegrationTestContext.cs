using System.Reflection;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

[UsedImplicitly]
public class IntegrationTestContext
{
    public readonly string SolutionRootPath = GetSolutionRoot();
    public const string BuildConfiguration = "Release";
    private readonly object _lock = new();
    private bool _initialized;
    private Exception? _initializationException;

    public void EnsureInitialized(ITestOutputHelper output)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                if (_initializationException != null) throw _initializationException;
                return;
            }

            try
            {
                BuildRuntime(output);
                BuildCompiler(output);
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

    private void BuildRuntime(ITestOutputHelper output)
    {
        var runtimeProjectFile = Path.Combine(SolutionRootPath, "Cesium.Runtime/Cesium.Runtime.csproj");
        BuildDotNetProject(output, runtimeProjectFile);
    }

    private void BuildCompiler(ITestOutputHelper output)
    {
        var compilerProjectFile = Path.Combine(SolutionRootPath, "Cesium.Compiler/Cesium.Compiler.csproj");
        BuildDotNetProject(output, compilerProjectFile);
    }

    private void BuildDotNetProject(ITestOutputHelper output, string projectFilePath)
    {
        ExecUtil.RunToSuccess(output, "dotnet", Path.GetDirectoryName(projectFilePath)!, new[]
        {
            "build",
            "--configuration", BuildConfiguration,
            projectFilePath
        });
    }


}

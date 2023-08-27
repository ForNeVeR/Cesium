using System.Xml.Linq;
using System.Xml.XPath;
using Cesium.CodeGen;
using Xunit.Abstractions;

namespace Cesium.Test.Framework;

// TODO: Make a normal disposable class to delete the whole directory in the end of the test.
public static class CSharpCompilationUtil
{
    public static readonly TargetRuntimeDescriptor DefaultRuntime = TargetRuntimeDescriptor.Net60;
    private const string _configuration = "Debug";
    private const string _targetRuntime = "net6.0";
    private const string _projectName = "TestProject";

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

            var projectDirectory = await CreateCSharpProject(output, directory);
            await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Program.cs"), cSharpSource);
            await CompileCSharpProject(output, directory, _projectName);
            return Path.Combine(projectDirectory, "bin", _configuration, _targetRuntime, _projectName + ".dll");
        }
        finally
        {
            _testSemaphore.Release();
        }
    }

    private static async Task<string> CreateCSharpProject(ITestOutputHelper output, string directory)
    {
        await ExecUtil.RunToSuccess(
            output,
            "dotnet",
            directory,
            new[] { "new", "classlib", "--framework", _targetRuntime, "--output", _projectName });
        var projectDirectory = Path.Combine(directory, _projectName);
        var projectFilePath = Path.Combine(projectDirectory, $"{_projectName}.csproj");
        XDocument csProj;
        await using (var projectFileStream = new FileStream(projectFilePath, FileMode.Open, FileAccess.Read))
        {
            csProj = await XDocument.LoadAsync(projectFileStream, LoadOptions.None, CancellationToken.None);
        }

        var project = csProj.XPathSelectElement("/Project")!;
        project.Add(new XElement("PropertyGroup",
            new XElement(new XElement("AllowUnsafeBlocks", "true"))));

        var runtimeLibraryPath = GetCesiumRuntimeLibraryPath();
        project.Add(new XElement("ItemGroup",
            new XElement("Reference", new XAttribute("Include", runtimeLibraryPath))));

        await using var outputStream = new FileStream(projectFilePath, FileMode.Truncate, FileAccess.Write);
        await csProj.SaveAsync(outputStream, SaveOptions.None, CancellationToken.None);

        return projectDirectory;
    }

    private static string GetCesiumRuntimeLibraryPath() => Path.Combine(
        TestStructureUtil.SolutionRootPath,
        "Cesium.Runtime",
        "bin",
        _configuration,
        _targetRuntime,
        "Cesium.Runtime.dll");

    private static Task CompileCSharpProject(ITestOutputHelper output, string directory, string projectName) =>
        ExecUtil.RunToSuccess(output, "dotnet", directory, new[]
        {
            "build",
            projectName,
            "--configuration", _configuration,
        });
}

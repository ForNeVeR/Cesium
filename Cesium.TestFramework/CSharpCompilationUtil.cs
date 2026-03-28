// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Xml.Linq;
using System.Xml.XPath;
using AsyncKeyedLock;
using Cesium.CodeGen;
using Cesium.Solution.Metadata;
using TruePath;
using TruePath.SystemIo;
using Xunit.Abstractions;

namespace Cesium.TestFramework;

public class CSharpCompilationUtil : IDisposable
{
    public static readonly TargetRuntimeDescriptor DefaultRuntime = TargetRuntimeDescriptor.Net60;
    private const string _configuration = "Debug";
    private const string _targetRuntime = "net10.0";
    private const string _cesiumRuntimeLibTargetRuntime = "net6.0";
    private const string _projectName = "TestProject";

    /// <summary>Semaphore that controls the number of simultaneously running tests.</summary>
    private static readonly AsyncNonKeyedLocker _testSemaphore = new(Environment.ProcessorCount);
    public AbsolutePath TempDirectory { get; } = Temporary.CreateTempFolder();

    public async Task<AbsolutePath> CompileCSharpAssembly(
        ITestOutputHelper output,
        TargetRuntimeDescriptor runtime,
        IEnumerable<AbsolutePath> references,
        string cSharpSource,
        bool isApplication = false)
    {
        if (runtime != DefaultRuntime) throw new Exception($"Runtime {runtime} not supported for test compilation.");

        using (await _testSemaphore.LockAsync())
        {
            var projectDirectory = await CreateCSharpProject(output, isApplication, TempDirectory, references);
            await File.WriteAllTextAsync((projectDirectory / "Program.cs").Value, cSharpSource);
            await CompileCSharpProject(output, TempDirectory, _projectName);
            return projectDirectory / "bin" / _configuration / _targetRuntime / (_projectName + ".dll");
        }
    }

    private static async Task<AbsolutePath> CreateCSharpProject(
        ITestOutputHelper output,
        bool isApplication,
        AbsolutePath directory,
        IEnumerable<AbsolutePath> references)
    {
        await ExecUtil.RunToSuccess(
            output,
            ExecUtil.DotNetHost,
            directory,
            ["new", isApplication ? "console" : "classlib", "--framework", _targetRuntime, "--output", _projectName]);
        var projectDirectory = directory / _projectName;
        var projectFilePath = projectDirectory / $"{_projectName}.csproj";
        XDocument csProj;
        await using (var projectFileStream = new FileStream(projectFilePath.Value, FileMode.Open, FileAccess.Read))
        {
            csProj = await XDocument.LoadAsync(projectFileStream, LoadOptions.None, CancellationToken.None);
        }

        var project = csProj.XPathSelectElement("/Project")!;
        project.Add(new XElement("PropertyGroup",
            new XElement(new XElement("AllowUnsafeBlocks", "true"))));

        project.Add(
            new XElement("ItemGroup", [
                new XElement("Reference",
                    new XAttribute("Include", CesiumRuntimeLibraryPath),
                    new XAttribute("Private", "true")),
                ..references.Select(r => new XElement("Reference",
                    new XAttribute("Include", r.Value),
                    new XAttribute("Private", "true")))
            ]));

        await using var outputStream = new FileStream(projectFilePath.Value, FileMode.Truncate, FileAccess.Write);
        await csProj.SaveAsync(outputStream, SaveOptions.None, CancellationToken.None);

        return projectDirectory;
    }

    public static readonly AbsolutePath CesiumRuntimeLibraryPath =
        SolutionMetadata.ArtifactsRoot
        / "bin"
        / "Cesium.Runtime"
        / $"{_configuration.ToLower()}_{_cesiumRuntimeLibTargetRuntime}"
        / "Cesium.Runtime.dll";

    private static Task CompileCSharpProject(ITestOutputHelper output, AbsolutePath directory, string projectName) =>
        ExecUtil.RunToSuccess(output, ExecUtil.DotNetHost, directory, [
            "build",
            projectName,
            "--configuration", _configuration
        ]);

    public void Dispose()
    {
        TempDirectory.DeleteDirectoryRecursively();
    }
}

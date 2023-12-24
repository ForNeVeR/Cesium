using System.Collections.Generic;
using System.IO;
using NuGet.Packaging;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Project = Microsoft.Build.Evaluation.Project;

public partial class Build
{
    Target PublishCompilerPacks => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();
            var runtimeIds = compilerProject.GetProperty("RuntimeIdentifiers").EvaluatedValue.Split(";");
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            foreach (var runtimeId in runtimeIds)
            {
                Log.Information($"Publishing for {runtimeId}...");
                DotNetPublish(o => o
                    .SetConfiguration(Configuration)
                    .SetProject(compilerProject.ProjectFileLocation.File)
                    .SetRuntime(runtimeId)
                    .SetOutput(GetCompilerRuntimePublishFolder(compilerProject, runtimeId))
                    .EnableNoBuild()
                    .EnableNoRestore());
            }
        });

    Target PackCompilerPacks => _ => _
        .Executes(() =>
        {
            var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();
            var runtimeIds = compilerProject.GetProperty("RuntimeIdentifiers").EvaluatedValue.Split(";");
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            foreach (var runtimeId in runtimeIds)
            {
                Log.Information($"Packing compiler for {runtimeId}...");
                EmitCompilerPack(runtimeId, compilerProject);
            }
        });

    static void EmitCompilerPack(string runtimeId, Project compilerProject)
    {
        var packageId = $"Cesium.Compiler.Pack.{runtimeId}";
        var packageFile = $"{packageId}.nupkg";
        var publishDirectory = GetCompilerRuntimePublishFolder(compilerProject, runtimeId);
        var publishedFiles = Directory.GetFiles(publishDirectory, "*.*", SearchOption.AllDirectories);
        var packageOutputPath = compilerProject.GetProperty("PackageOutputPath").EvaluatedValue;

        Log.Debug($"Source publish directory: {publishDirectory}");
        Log.Debug($"Target package ID: {packageId}");
        Log.Debug($"Target package output directory: {packageOutputPath}");

        var builder = new PackageBuilder
        {
            Id = packageId,
            Version = NuGetVersion.Parse(compilerProject.GetProperty("VersionPrefix").EvaluatedValue),
            Description = $"Cesium compiler native executable pack for {runtimeId} platform.",
            Authors = { "Cesium Team" }
        };
        builder.Files.AddRange(GetPhysicalFiles(publishDirectory, publishedFiles));

        var packageFileName = Path.Combine(packageOutputPath, packageFile);
        Log.Information($"Package is ready, saving to {packageFileName}...");
        Directory.CreateDirectory(packageOutputPath);
        using var outputStream = new FileStream(packageFileName, FileMode.Create);
        builder.Save(outputStream);
        return;

        IEnumerable<IPackageFile> GetPhysicalFiles(string publishDirectory, IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                yield return new PhysicalPackageFile
                {
                    SourcePath = filePath,
                    TargetPath = $"tools/{Path.GetRelativePath(publishDirectory, filePath)}"
                };
            }
        }
    }

    static string GetCompilerRuntimePublishFolder(Project compilerProject, string runtimeId) =>
        Path.Combine(
            compilerProject.GetProperty("ArtifactsPath").EvaluatedValue,
            compilerProject.GetProperty("ArtifactsPublishOutputName").EvaluatedValue,
            GetRuntimeArtifactFolder(runtimeId));

    static string GetRuntimeArtifactFolder(string runtimeId) => $"pack_{runtimeId}";
}

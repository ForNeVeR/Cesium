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
    const string _compilerPackPackagePrefix = "Cesium.Compiler.Pack";

    Target PublishCompilerPacks => _ => _
        .Executes(() =>
        {
            var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();
            var runtimeIds = compilerProject.GetProperty("RuntimeIdentifiers").EvaluatedValue.Split(";");

            var runtimeIds = compilerProject.GetEvaluatedProperty("RuntimeIdentifiers").Split(";");
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            if (!string.IsNullOrEmpty(RuntimeId))
            {
                Log.Information($"Executing only {RuntimeId} because it was specified explicitly.");
                PublishCompiler(RuntimeId);
                return;
            }

            foreach (var runtimeId in runtimeIds)
                PublishCompiler(runtimeId);

            void PublishCompiler(string runtimeId)
            {
                Log.Information(SkipCaches+"");
                if (!SkipCaches && !NeedPublishCompilerPack(compilerProject, runtimeId))
                {
                    Log.Information($"Skipping {runtimeId} because it was already published. Use '--skip-caches true' to re-publish.");
                    return;
                }

                Log.Information($"Publishing for {runtimeId}...");
                DotNetPublish(o => o
                    .SetConfiguration(Configuration)
                    .SetProject(compilerProject.ProjectFileLocation.File)
                    .SetRuntime(runtimeId)
                    .SetOutput(GetCompilerRuntimePublishFolder(compilerProject, runtimeId)));
            }
        });

    Target PackCompilerPacks => _ => _
        .DependsOn(PublishCompilerPacks)
        .Executes(() =>
        {
            var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

            var runtimeIds = compilerProject.GetRuntimeIds();
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            if (!string.IsNullOrEmpty(RuntimeId))
            {
                Log.Information($"Executing only {RuntimeId} because it was specified explicitly.");
                PackCompiler(RuntimeId);
                return;
            }

            foreach (var runtimeId in runtimeIds)
                PackCompiler(runtimeId);

            void PackCompiler(string runtimeId)
            {
                if (!SkipCaches && !NeedPackageCompilerPack(compilerProject, runtimeId))
                {
                    Log.Information($"Skipping {runtimeId} because it was already packed. Use '--skip-caches true' to re-pack.");
                    return;
                }

                Log.Information($"Packing compiler for {runtimeId}...");
                EmitCompilerPack(runtimeId, compilerProject);
            }
        });

    Target PackSdk => _ => _
        .Executes(() =>
        {
            var sdkProject = Solution.Cesium_Sdk.GetMSBuildProject();
            if (!SkipCaches && !NeedPackageSdk(sdkProject))
            {
                Log.Information($"Skipping SDK packing because it was already packed. Use '--skip-caches true' to re-publish.");
                return;
            }

            Log.Information($"Packing SDK...");
            DotNetPack(o => o
                .SetConfiguration(Configuration)
                .SetProject(Solution.Cesium_Sdk.Path));
        });

    void EmitCompilerPack(string runtimeId, Project compilerProject)
    {
        var version = compilerProject.GetVersion();
        var runtimePackageId = GetRuntimePackId(runtimeId);
        var packageFile = GetRuntimePackFileName(version, runtimeId);
        var publishDirectory = GetCompilerRuntimePublishFolder(compilerProject, runtimeId);
        Directory.CreateDirectory(publishDirectory);
        var publishedFiles = Directory.GetFiles(publishDirectory, "*.*", SearchOption.AllDirectories);
        var packageOutputPath = compilerProject.GetPackageOutputPath();

        Log.Debug($"Source publish directory: {publishDirectory}");
        Log.Debug($"Target package ID: {runtimePackageId}");
        Log.Debug($"Target package output directory: {packageOutputPath}");

        var builder = new PackageBuilder
        {
            Id = runtimePackageId,
            Version = NuGetVersion.Parse(compilerProject.GetVersion()),
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

    string GetCompilerRuntimePublishFolder(Project compilerProject, string runtimeId) =>
        Path.Combine(
            compilerProject.GetProperty("ArtifactsPath").EvaluatedValue,
            compilerProject.GetProperty("ArtifactsPublishOutputName").EvaluatedValue,
            Solution.Cesium_Compiler.Name,
            GetRuntimeArtifactFolder(compilerProject.GetVersion(), runtimeId));

    static string GetRuntimeArtifactFolder(string version, string runtimeId) =>
        $"pack_{version}_{runtimeId}";

    static string GetRuntimePackId(string runtimeId) =>
        $"{_compilerPackPackagePrefix}.{runtimeId}";

    static string GetRuntimePackFileName(string version, string runtimeId) =>
        $"{_compilerPackPackagePrefix}.{runtimeId}.{version}.nupkg";

    bool NeedPublishCompilerPack(Project compiler, string runtimeId)
    {
        var folder = GetCompilerRuntimePublishFolder(compiler, runtimeId);

        return !Directory.Exists(folder)
               || Directory.GetFiles(folder, "Cesium.Compiler*").Length == 0;
    }

    bool NeedPackageCompilerPack(Project compiler, string runtimeId)
    {
        var version = compiler.GetVersion();
        var packageDirectory = compiler.GetPackageOutputPath();
        var packageFileName = GetRuntimePackFileName(version, runtimeId);

        return !File.Exists(Path.Combine(packageDirectory, packageFileName));
    }

    bool NeedPackageSdk(Project sdk)
    {
        var packageId = sdk.GetProperty("PackageVersion").EvaluatedValue;
        var version = sdk.GetProperty("PackageId").EvaluatedValue;
        var packageDirectory = sdk.GetPackageOutputPath();
        var packageFileName = $"{packageId}.{version}";

        return !File.Exists(Path.Combine(packageDirectory, packageFileName));
    }
}

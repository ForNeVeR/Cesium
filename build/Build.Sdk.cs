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
    const string _compilerBundlePackagePrefix = "Cesium.Compiler.Bundle";

    Target PublishAllCompilerBundles => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

            var runtimeIds = compilerProject.GetEvaluatedProperty("RuntimeIdentifiers").Split(";");
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            foreach (var runtimeId in runtimeIds)
                PublishCompiler(runtimeId);
        });

    Target PublishCompilerBundle => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            PublishCompiler(EffectiveRuntimeId);
        });

    Target PackAllCompilerBundles => _ => _
        .DependsOn(PublishAllCompilerBundles)
        .Executes(() =>
        {
            var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

            var runtimeIds = compilerProject.GetRuntimeIds();
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            foreach (var runtimeId in runtimeIds)
                PackCompiler(runtimeId);
        });

    Target PackCompilerBundle => _ => _
        .DependsOn(PublishCompilerBundle)
        .Executes(() =>
        {
            PackCompiler(EffectiveRuntimeId);
        });

    Target PackSdk => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            var sdkProject = Solution.Cesium_Sdk.GetMSBuildProject();
            if (!SkipCaches && !NeedPackageSdk(sdkProject))
            {
                Log.Information($"Skipping SDK packing because it was already packed. Use '--skip-caches true' to re-publish.");
                return;
            }

            Log.Information("Packing SDK...");
            DotNetPack(o => o
                .SetConfiguration(Configuration)
                .SetProject(Solution.Cesium_Sdk.Path));
        });

    void EmitCompilerBundle(string runtimeId, Project compilerProject)
    {
        var version = compilerProject.GetVersion();
        var runtimePackageId = GetRuntimeBundleId(runtimeId);
        var packageFile = GetRuntimeBundleFileName(version, runtimeId);
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

    void PublishCompiler(string runtimeId)
    {
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

        if (!SkipCaches && !NeedPublishCompilerBundle(compilerProject, runtimeId))
        {
            Log.Information($"Skipping {runtimeId} because it was already published. Use '--skip-caches true' to re-publish.");
            return;
        }

        Log.Information($"Publishing for {runtimeId}, AOT {(PublishAot ? "enabled" : "disabled")}...");
        DotNetPublish(o => o
            .SetConfiguration(Configuration)
            .SetProject(compilerProject.ProjectFileLocation.File)
            .SetRuntime(runtimeId)
            .SetSelfContained(true)
            .SetPublishTrimmed(PublishAot)
            .SetPublishSingleFile(PublishAot)
            .SetProperty("PublishAot", PublishAot)
            .SetOutput(GetCompilerRuntimePublishFolder(compilerProject, runtimeId)));
    }

    void PackCompiler(string runtimeId)
    {
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

        if (!SkipCaches && !NeedPackageCompilerBundle(compilerProject, runtimeId))
        {
            Log.Information($"Skipping {runtimeId} because it was already packed. Use '--skip-caches true' to re-pack.");
            return;
        }

        Log.Information($"Packing compiler for {runtimeId}...");
        EmitCompilerBundle(runtimeId, compilerProject);
    }

    string GetCompilerRuntimePublishFolder(Project compilerProject, string runtimeId) =>
        Path.Combine(
            compilerProject.GetProperty("ArtifactsPath").EvaluatedValue,
            compilerProject.GetProperty("ArtifactsPublishOutputName").EvaluatedValue,
            Solution.Cesium_Compiler.Name,
            GetRuntimeArtifactFolder(compilerProject.GetVersion(), runtimeId));

    static string GetRuntimeArtifactFolder(string version, string runtimeId) =>
        $"pack_{version}_{runtimeId}";

    static string GetRuntimeBundleId(string runtimeId) =>
        $"{_compilerBundlePackagePrefix}.{runtimeId}";

    static string GetRuntimeBundleFileName(string version, string runtimeId) =>
        $"{_compilerBundlePackagePrefix}.{runtimeId}.{version}.nupkg";

    bool NeedPublishCompilerBundle(Project compiler, string runtimeId)
    {
        var folder = GetCompilerRuntimePublishFolder(compiler, runtimeId);

        return !Directory.Exists(folder)
               || Directory.GetFiles(folder, "Cesium.Compiler*").Length == 0;
    }

    bool NeedPackageCompilerBundle(Project compiler, string runtimeId)
    {
        var version = compiler.GetVersion();
        var packageDirectory = compiler.GetPackageOutputPath();
        var packageFileName = GetRuntimeBundleFileName(version, runtimeId);

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

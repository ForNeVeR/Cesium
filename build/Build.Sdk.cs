// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Project = Microsoft.Build.Evaluation.Project;

public partial class Build
{
    private const string _compilerBundlePackageName = "Cesium.Compiler.Bundle";

    private static readonly string[] _compilerRuntimeSpecificRuntimeIds = [
        "win-x64",
        "win-x86",
        "win-arm64",
        "linux-x64",
        "linux-arm64",
        "osx-x64",
        "osx-arm64"
    ];

    Target PublishAllCompilerRuntimeSpecificBundles => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            var runtimeIds = _compilerRuntimeSpecificRuntimeIds;
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            foreach (var runtimeId in runtimeIds)
                PublishCompiler(runtimeId);
        });

    Target PublishCompilerFrameworkDependentBundle => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            PublishCompiler(null);
        });

    Target PackAllCompilerRuntimeSpecificBundles => _ => _
        .DependsOn(PublishAllCompilerRuntimeSpecificBundles)
        .Executes(() =>
        {
            var runtimeIds = _compilerRuntimeSpecificRuntimeIds;
            Log.Information(
                $"Runtime identifiers defined in {Solution.Cesium_Compiler.Name}: {string.Join(", ", runtimeIds)}");

            foreach (var runtimeId in runtimeIds)
                GenerateCompilerRuntimeSpecificBundle(runtimeId);
        });

    Target PackCompilerNuPkg => _ => _
        .DependsOn(PublishCompilerFrameworkDependentBundle)
        .Executes(GenerateCompilerNuPkg);

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

    void GenerateCompilerRuntimeSpecificBundle(string runtimeId)
    {
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

        if (!SkipCaches && !NeedPackageCompilerRuntimeSpecificBundle(compilerProject, runtimeId))
        {
            Log.Information($"Skipping {runtimeId} because it was already packed. Use '--skip-caches true' to re-pack.");
            return;
        }

        Log.Information($"Generating compiler runtime specific bundle {runtimeId}...");

        var version = compilerProject.GetVersion();
        var runtimePackageId = GetRuntimeBundleId(runtimeId);
        var packageFile = GetCompilerRuntimeSpecificBundleFileName(version, runtimeId);
        var publishDirectory = GetCompilerRuntimePublishFolder(compilerProject, runtimeId);
        Directory.CreateDirectory(publishDirectory);

        var packageOutputPath = compilerProject.GetPackageOutputPath();

        Log.Debug($"Source publish directory: {publishDirectory}");
        Log.Debug($"Target package ID: {runtimePackageId}");
        Log.Debug($"Target package output directory: {packageOutputPath}");

        var packageFileName = Path.Combine(packageOutputPath, packageFile);
        Log.Information($"Package is ready, saving to {packageFileName}...");
        Directory.CreateDirectory(packageOutputPath);
        publishDirectory.ZipTo(
            packageFileName,
            fileMode: FileMode.CreateNew
        );
    }

    void GenerateCompilerNuPkg()
    {
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject();

        if (!SkipCaches && !NeedPackageCompilerNuPkg(compilerProject))
        {
            Log.Information("Skipping .nupkg because it was already packed. Use '--skip-caches true' to re-pack.");
            return;
        }

        Log.Information("Packing compiler .nupkg file...");
        DotNetPack(o => o
            .SetConfiguration(Configuration)
            .SetProject(compilerProject.ProjectFileLocation.File)
            .SetProperty("PublishAot", PublishAot));
    }

    AbsolutePath GetCompilerRuntimePublishFolder(Project compilerProject, string? runtimeId) =>
        Path.Combine(
            compilerProject.GetProperty("ArtifactsPath").EvaluatedValue,
            compilerProject.GetProperty("ArtifactsPublishOutputName").EvaluatedValue,
            Solution.Cesium_Compiler.Name,
            GetRuntimeArtifactFolder(compilerProject.GetVersion(), runtimeId));

    static string GetRuntimeArtifactFolder(string version, string? runtimeId) =>
        $"pack_{version}" + (runtimeId == null ? "" : $"_{runtimeId}");

    static string GetRuntimeBundleId(string runtimeId) =>
        $"{_compilerBundlePackageName}.{runtimeId}";

    static string GetCompilerNuGetPackageFileName(string version) =>
        $"{_compilerBundlePackageName}.{version}.nupkg";

    static string GetCompilerRuntimeSpecificBundleFileName(string version, string runtimeId) =>
        $"{_compilerBundlePackageName}.{runtimeId}.{version}.zip";

    bool NeedPublishCompilerBundle(Project compiler, string runtimeId)
    {
        var folder = GetCompilerRuntimePublishFolder(compiler, runtimeId);

        return !Directory.Exists(folder)
               || Directory.GetFiles(folder, "Cesium.Compiler*").Length == 0;
    }

    bool NeedPackageCompilerRuntimeSpecificBundle(Project compiler, string runtimeId)
    {
        var version = compiler.GetVersion();
        var packageDirectory = compiler.GetPackageOutputPath();
        var packageFileName = GetCompilerRuntimeSpecificBundleFileName(version, runtimeId);

        return !File.Exists(Path.Combine(packageDirectory, packageFileName));
    }

    bool NeedPackageCompilerNuPkg(Project compiler)
    {
        var version = compiler.GetVersion();
        var packageDirectory = compiler.GetPackageOutputPath();
        var packageFileName = GetCompilerNuGetPackageFileName(version);

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

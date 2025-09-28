// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Licenses;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
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

    Target PackCompilerTool => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            var project = Solution.Cesium_Compiler.GetMSBuildProject(configuration: Configuration);
            if (!SkipCaches && !NeedPackageSdk(project))
            {
                Log.Information($"Skipping the compiler packing because it was already packed. Use '--skip-caches true' to re-pack.");
                return;
            }

            Log.Information("Packing the compiler…");
            DotNetPack(o => o
                .SetConfiguration(Configuration)
                .SetProject(Solution.Cesium_Compiler)
                .SetProperty("PublishTrimmed", false)); // cannot pack a portable tool trimmed
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

    Target PackCompilerBundleNuPkg => _ => _
        .DependsOn(PublishCompilerFrameworkDependentBundle)
        .Executes(GenerateCompilerNuPkg);

    Target PackSdk => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            var sdkProject = Solution.Cesium_Sdk.GetMSBuildProject(configuration: Configuration);
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

    void PublishCompiler(string? runtimeId)
    {
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject(configuration: Configuration);
        var runtimeIdDisplayName = runtimeId ?? "<no runtime>";

        if (!SkipCaches && !NeedPublishCompilerBundle(compilerProject, runtimeId))
        {
            Log.Information($"Skipping {runtimeIdDisplayName} because it was already published. Use '--skip-caches true' to re-publish.");
            return;
        }

        Log.Information($"Publishing for {runtimeIdDisplayName}, AOT {(PublishAot ? "enabled" : "disabled")}...");
        DotNetPublish(o => o
            .SetConfiguration(Configuration)
            .SetProject(compilerProject.ProjectFileLocation.File)
            .SetRuntime(runtimeId)
            .SetSelfContained(runtimeId != null) // self-contained for runtime-specific only
            .SetPublishTrimmed(PublishAot)
            .SetPublishSingleFile(PublishAot)
            .SetProperty("PublishAot", PublishAot)
            .SetOutput(GetCompilerRuntimePublishFolder(compilerProject, runtimeId)));
    }

    void GenerateCompilerRuntimeSpecificBundle(string runtimeId)
    {
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject(configuration: Configuration);

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
        var compilerProject = Solution.Cesium_Compiler.GetMSBuildProject(configuration: Configuration);

        if (!SkipCaches && !NeedPackageCompilerNuPkg(compilerProject))
        {
            Log.Information("Skipping .nupkg because it was already packed. Use '--skip-caches true' to re-pack.");
            return;
        }

        var packageId = _compilerBundlePackageName;
        var version = compilerProject.GetVersion();
        var packageFile = GetCompilerNuGetPackageFileName(version);
        var publishDirectory = GetCompilerRuntimePublishFolder(compilerProject, null);
        var publishedFiles = Directory.GetFiles(publishDirectory, "*.*", SearchOption.AllDirectories);
        var packageOutputPath = compilerProject.GetPackageOutputPath();

        Log.Debug($"Source publish directory: {publishDirectory}");
        Log.Debug($"Target package ID: {packageId}");
        Log.Debug($"Target package output directory: {packageOutputPath}");

        var builder = new PackageBuilder
        {
            Id = packageId,
            Version = NuGetVersion.Parse(compilerProject.GetVersion()),
            Description = "Cesium compiler executable bundle. Used by the Cesium SDK.",
            LicenseMetadata = new LicenseMetadata(
                LicenseType.Expression,
                GetCompilerBundleLicenseExpression(compilerProject),
                null,
                null,
                LicenseMetadata.CurrentVersion),
            Authors = { "Cesium contributors" },
            Copyright = string.Join(";\n", GetCompilerBundleCopyrightStatements(compilerProject).Distinct())
        };
        builder.Files.AddRange(GetPhysicalFiles(publishDirectory, publishedFiles));

        var packageFileName = Path.Combine(packageOutputPath, packageFile);
        Log.Information($"Package is ready, saving to {packageFileName}…");
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

    private string? GetNuPkgPath(string packageId, string version)
    {
        var v = NuGetVersion.Parse(version);

        var settings = Settings.LoadDefaultSettings(root: Directory.GetCurrentDirectory());
        var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

        var packagePathResolver = new VersionFolderPathResolver(globalPackagesFolder);
        var nupkgPath = packagePathResolver.GetPackageFilePath(packageId, v);

        if (!File.Exists(nupkgPath))
            return null;

        return nupkgPath;
    }

    private string? GetPackageCopyright(string packageId, string version)
    {
        var nupkgPath = GetNuPkgPath(packageId, version);
        if (nupkgPath == null) throw new Exception($"Cannot find .nupkg file for package {packageId} {version}.");

        using var reader = new PackageArchiveReader(nupkgPath);
        var nuspec = reader.NuspecReader;
        return nuspec.GetCopyright();
    }

    private string? GetPackageLicenseExpression(string packageId, string version)
    {
        // TODO[#839]: These are unused, hopefully will go away after we use dotnet-licenses.
        switch (packageId)
        {
            case "Microsoft.VisualStudio.Setup.Configuration.Interop": return null;
        }

        switch (packageId, version)
        {
            case ("CommandLineParser", "2.9.1"): return "MIT"; // Sadly, the package is not actively maintained anymore, we'll have to live with it here
        }

        var nupkgPath = GetNuPkgPath(packageId, version);
        if (nupkgPath == null) throw new Exception($"Cannot find .nupkg file for package {packageId} {version}.");

        using var reader = new PackageArchiveReader(nupkgPath);
        var nuspec = reader.NuspecReader;
        var metadata = nuspec.GetLicenseMetadata();
        if (metadata == null)
        {
            var licenseUrl = nuspec.GetLicenseUrl();
            return licenseUrl switch
            {
                "https://github.com/dotnet/corefx/blob/master/LICENSE.TXT" => "MIT",
                _ => throw new Exception(
                    $"Cannot find the license metadata for the package {packageId} {version} ({nupkgPath}). License URL: {licenseUrl}.")
            };
        }

        var expression = metadata.LicenseExpression;
        if (expression == null) throw new Exception(
            $"Cannot read the license expression for the package {packageId} {version} ({nupkgPath})." +
            $" Metadata: {metadata}.");
        return expression.ToString();
    }

    private IEnumerable<(string Id, string Version)> GetCompilerBundlePackages(Project project)
    {
        var json = DotNet($"list \"{project.FullPath}\" package --include-transitive --format json", logOutput: false)
            .StdToText();

        using var doc = JsonDocument.Parse(json);
        var allPackages =
            doc.RootElement
                .GetProperty("projects")[0]
                .GetProperty("frameworks")[0]
                .GetProperty("topLevelPackages")
                .EnumerateArray()
                .Concat(doc.RootElement
                    .GetProperty("projects")[0]
                    .GetProperty("frameworks")[0]
                    .GetProperty("transitivePackages")
                    .EnumerateArray())
                .Select(e => (Id: e.GetProperty("id").GetString()!, Version: e.GetProperty("resolvedVersion").GetString()!))
                .ToList();

        // TODO[#839]: Currently, this yields too many packages that aren't in the bundle (e.g. System.Memory,
        //             Changelog.Automation). In the future, I hope that dotnet.licenses will generate this better. But
        //             for now, this will have to work.

        return allPackages;
    }

    private string GetCompilerBundleLicenseExpression(Project project)
    {
        var licenseExpressions = GetLicenseExpressions().Distinct().OrderBy(x => x).ToList();
        var result = string.Join(" AND ", licenseExpressions);
        var expectedLicenseExpression = "Apache-2.0 AND BSD-2-Clause AND MIT AND MS-PL";
        if (result != expectedLicenseExpression)
        {
            throw new Exception(
                $"Expected the combined license expression to be {expectedLicenseExpression}, but was  {result}." +
                " Please adjust as necessary.");
        }

        return result;

        IEnumerable<string> GetLicenseExpressions()
        {
            yield return "MIT";

            foreach (var package in GetCompilerBundlePackages(project))
            {
                var license = GetPackageLicenseExpression(package.Id, package.Version);
                if (license != null)
                {
                    // NOTE: Of all our dependencies, there's only one crazy dep that says "MIT AND BSD-2-Clause", and
                    // that's ChangelogAutomation.MSBuild. Here goes a very particular workaround for that one:
                    if (license.Contains(' '))
                    {
                        var expression = NuGetLicenseExpression.Parse(license);

                        if (expression is LogicalOperator { LogicalOperatorType: LogicalOperatorType.And } op)
                        {
                            yield return op.Left.ToString()!;
                            yield return op.Right.ToString()!;
                            continue;
                        }
                    }

                    yield return license;
                }
            }
        }
    }

    private IEnumerable<string> GetCompilerBundleCopyrightStatements(Project project)
    {
        yield return "2021-2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>";

        foreach (var package in GetCompilerBundlePackages(project))
        {
            var copyright = GetPackageCopyright(package.Id, package.Version)?.Trim();
            if (!string.IsNullOrEmpty(copyright))
                yield return $"{copyright} ({package.Id})";
        }
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

    bool NeedPublishCompilerBundle(Project compiler, string? runtimeId)
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

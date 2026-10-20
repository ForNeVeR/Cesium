// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cesium.Solution.Metadata;
using Cesium.TestFramework;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public abstract class SdkTestBase : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _temporaryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly Dictionary<string, string> _dotNetEnvVars;

    private string NuGetConfigPath => Path.Combine(_temporaryPath, "NuGet.config");
    private string GlobalJsonPath => Path.Combine(_temporaryPath, "global.json");

    protected SdkTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _dotNetEnvVars = new() { ["NUGET_PACKAGES"] = Path.Combine(_temporaryPath, "package-cache") };

        File.Delete(_temporaryPath);

        _testOutputHelper.WriteLine($"Test projects folder: {_temporaryPath}");

        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var testDataPath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "TestProjects");
        _testOutputHelper.WriteLine($"Copying TestProjects to {_temporaryPath}...");
        CopyDirectoryRecursive(testDataPath, _temporaryPath);

        var nupkgPath = (SolutionMetadata.SourceRoot / "artifacts/package/debug").Canonicalize();
        _testOutputHelper.WriteLine($"Local NuGet feed: {nupkgPath}.");
        EmitNuGetConfig(NuGetConfigPath, nupkgPath);
        EmitGlobalJson(GlobalJsonPath, $"{SolutionMetadata.VersionPrefix}");
    }

    protected async Task<BuildResult> ExecuteTargets(string projectName, params string[] targets)
    {
        return await ExecuteTargets($"{projectName}/{projectName}.ceproj", $"{projectName}/{projectName}.ceproj", projectName, "OutDir", targets, ["/restore"]);
    }

    protected async Task<BuildResult> ExecuteTargets(string buildProjectFile, string validatingProjectFile, string testName, string outputProperty, string[] targets, string[] switches)
    {
        var joinedTargets = string.Join(";", targets);
        var testProjectFile = Path.GetFullPath(Path.Combine(_temporaryPath, buildProjectFile));
        var testEntryProjectFile = Path.GetFullPath(Path.Combine(_temporaryPath, validatingProjectFile));
        var testProjectFolder = Path.GetDirectoryName(testProjectFile) ?? throw new ArgumentNullException(nameof(testProjectFile));
        var validatingProjectFolder = Path.GetDirectoryName(testEntryProjectFile) ?? throw new ArgumentNullException(nameof(validatingProjectFile));
        var binLogFile = Path.Combine(testProjectFolder, $"build_result_{testName}_{DateTime.UtcNow:yyyy-dd-M_HH-mm-s}.binlog");

        const string objFolderPropertyName = "IntermediateOutputPath";

        foreach (var (name, var) in _dotNetEnvVars)
        {
            _testOutputHelper.WriteLine($"Environment[{name}] = {var}");
        }

        var processResult = await ExecUtil.Run(
            _testOutputHelper,
            ExecUtil.DotNetHost,
            new AbsolutePath(testProjectFolder),
            ["msbuild", testProjectFile, $"/t:{joinedTargets}", $"/bl:{binLogFile}", ..switches],
            additionalEnvironment: _dotNetEnvVars);

        _testOutputHelper.WriteLine(processResult.Success
            ? "Build succeeded"
            : $"Build failed with exit code {processResult.ExitCode}");

        var properties = await DotNetCliHelper.EvaluateMSBuildProperties(
            _testOutputHelper,
            testEntryProjectFile,
            env: _dotNetEnvVars,
            switches,
            [objFolderPropertyName, outputProperty]);
        _testOutputHelper.WriteLine($"Properties request result: {JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = false })}");

        var binFolder = NormalizePath(Path.GetFullPath(properties[outputProperty], validatingProjectFolder));
        var objFolder = NormalizePath(Path.GetFullPath(properties[objFolderPropertyName], validatingProjectFolder));

        var binArtifacts = CollectArtifacts(binFolder);
        var objArtifacts = CollectArtifacts(objFolder);

        var result = new BuildResult(
            processResult.ExitCode,
            processResult.StandardOutput,
            processResult.StandardError,
            binArtifacts,
            objArtifacts);
        _testOutputHelper.WriteLine($"Build result: {JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })}");
        return result;

        IReadOnlyCollection<BuildArtifact> CollectArtifacts(string folder)
        {
            _testOutputHelper.WriteLine($"Collecting artifacts from '{folder}' folder");
            return Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Select(path => new BuildArtifact(Path.GetRelativePath(folder, path), path))
                    .ToList()
                : Array.Empty<BuildArtifact>();
        }
    }

    protected async Task<IEnumerable<string>> ListItems(string projectName, string itemName)
    {
        var projectFile = $"{projectName}/{projectName}.ceproj";
        var testProjectFile = Path.GetFullPath(Path.Combine(_temporaryPath, projectFile));
        var items = await DotNetCliHelper.EvaluateMSBuildItem(_testOutputHelper, testProjectFile, itemName, env: _dotNetEnvVars);

        return items.Select(i => i.identity);
    }

    private static void EmitNuGetConfig(string configFilePath, AbsolutePath packageSourcePath)
    {
        File.WriteAllText(configFilePath, $"""
            <configuration>
                <packageSources>
                    <add key="local" value="{packageSourcePath.Value}" />
               </packageSources>
            </configuration>
            """);
    }

    private static void EmitGlobalJson(string globalJsonPath, string packageVersion)
    {
        var actualGlobalJson = SolutionMetadata.SourceRoot / "global.json";
        var globalConfig = JsonNode.Parse(File.ReadAllText(actualGlobalJson.Value))!;
        globalConfig["msbuild-sdks"] = new JsonObject([new KeyValuePair<string, JsonNode?>("Cesium.Sdk", packageVersion)]);
        var content = globalConfig.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(globalJsonPath, content);
    }

    private static void CopyDirectoryRecursive(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var subDirPath in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(subDirPath);
            CopyDirectoryRecursive(subDirPath, Path.Combine(target, dirName));
        }

        foreach (var filePath in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(filePath);
            File.Copy(filePath, Path.Combine(target, fileName));
        }
    }

    private static string NormalizePath(string path)
    {
        var normalizedPath = new Uri(path).LocalPath;
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? normalizedPath
            : normalizedPath.Replace('\\', '/');
    }

    protected record BuildResult(
        int ExitCode,
        string StdOutOutput,
        string StdErrOutput,
        IReadOnlyCollection<BuildArtifact> OutputArtifacts,
        IReadOnlyCollection<BuildArtifact> IntermediateArtifacts);

    protected record BuildArtifact(
        string FileName,
        string FullPath);

    private void ClearOutput()
    {
        Directory.Delete(_temporaryPath, true);
    }

    public void Dispose()
    {
        ClearOutput();
    }
}

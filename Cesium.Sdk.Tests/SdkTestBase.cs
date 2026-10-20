// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cesium.Solution.Metadata;
using Cesium.TestFramework;
using TruePath;
using TruePath.SystemIo;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public abstract class SdkTestBase : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    // Important to canonicalize — /tmp is typically a symlink on macOS, and if we call MSBuild on a non-canonical
    // path, this creates chaos, see #1010 for details.
    private readonly AbsolutePath _temporaryPath = Temporary.CreateTempFolder().Canonicalize();

    private readonly Dictionary<string, string> _dotNetEnvVars;

    private AbsolutePath NuGetConfigPath => _temporaryPath / "NuGet.config";
    private AbsolutePath GlobalJsonPath => _temporaryPath / "global.json";

    protected SdkTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _dotNetEnvVars = new() { ["NUGET_PACKAGES"] = (_temporaryPath / "package-cache").Value };

        _testOutputHelper.WriteLine($"Test projects folder: {_temporaryPath.Value}");

        var assemblyPath = new AbsolutePath(Assembly.GetExecutingAssembly().Location);
        var testDataPath = (assemblyPath.Parent ?? throw new Exception($"Cannot find parent directory of assembly {assemblyPath}")) / "TestProjects";
        _testOutputHelper.WriteLine($"Copying TestProjects to {_temporaryPath.Value}...");
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
        var testProjectFile = _temporaryPath / buildProjectFile;
        var testEntryProjectFile = _temporaryPath / validatingProjectFile;
        var testProjectFolder = testProjectFile.Parent ?? throw new ArgumentNullException(nameof(testProjectFile));
        var validatingProjectFolder = testEntryProjectFile.Parent ?? throw new ArgumentNullException(nameof(validatingProjectFile));
        var binLogFile = testProjectFolder / $"build_result_{testName}_{DateTime.UtcNow:yyyy-dd-M_HH-mm-s}.binlog";

        const string objFolderPropertyName = "IntermediateOutputPath";

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = testProjectFolder.Value,
            FileName = "dotnet",
            ArgumentList = { "msbuild", testProjectFile.Value, $"/t:{joinedTargets}", $"/bl:{binLogFile}" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        foreach (var commandLineOption in switches)
        {
            startInfo.ArgumentList.Add(commandLineOption);
        }

        _testOutputHelper.WriteLine("Running MSBuild: " + "dotnet " +string.Join(" ", startInfo.ArgumentList));
        foreach (var (name, var) in _dotNetEnvVars)
        {
            _testOutputHelper.WriteLine($"Environment[{name}] = {var}");
            startInfo.Environment[name] = var;
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        var stdOutOutput = "";
        var stdErrOutput = "";
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                stdOutOutput += e.Data + Environment.NewLine;
                _testOutputHelper.WriteLine($"[stdout]: {e.Data}");
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                stdErrOutput += e.Data + Environment.NewLine;
                _testOutputHelper.WriteLine($"[stderr]: {e.Data}");
            }
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var success = process.ExitCode == 0;

        _testOutputHelper.WriteLine(success
            ? "Build succeeded"
            : $"Build failed with exit code {process.ExitCode}");

        var properties = await DotNetCliHelper.EvaluateMSBuildProperties(
            _testOutputHelper,
            testEntryProjectFile,
            env: _dotNetEnvVars,
            switches,
            [objFolderPropertyName, outputProperty]);
        _testOutputHelper.WriteLine($"Properties request result: {JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = false })}");

        var binFolder = NormalizePath(Path.GetFullPath(properties[outputProperty], validatingProjectFolder.Value));
        var objFolder = NormalizePath(Path.GetFullPath(properties[objFolderPropertyName], validatingProjectFolder.Value));

        var binArtifacts = CollectArtifacts(binFolder);
        var objArtifacts = CollectArtifacts(objFolder);

        var result = new BuildResult(process.ExitCode, stdOutOutput, stdErrOutput, binArtifacts, objArtifacts);
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
        var testProjectFile = _temporaryPath / projectFile;
        var items = await DotNetCliHelper.EvaluateMSBuildItem(_testOutputHelper, testProjectFile, itemName, env: _dotNetEnvVars);

        return items.Select(i => i.identity);
    }

    private static void EmitNuGetConfig(AbsolutePath configFilePath, AbsolutePath packageSourcePath)
    {
        configFilePath.WriteAllText($"""
            <configuration>
                <packageSources>
                    <add key="local" value="{packageSourcePath.Value}" />
               </packageSources>
            </configuration>
            """);
    }

    private static void EmitGlobalJson(AbsolutePath globalJsonPath, string packageVersion)
    {
        var actualGlobalJson = SolutionMetadata.SourceRoot / "global.json";
        var globalConfig = JsonNode.Parse(actualGlobalJson.ReadAllText())!;
        globalConfig["msbuild-sdks"] = new JsonObject([new KeyValuePair<string, JsonNode?>("Cesium.Sdk", packageVersion)]);
        var content = globalConfig.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
        globalJsonPath.WriteAllText(content);
    }

    private static void CopyDirectoryRecursive(AbsolutePath source, AbsolutePath target)
    {
        target.CreateDirectory();

        foreach (var subDirPath in source.GetDirectories().Select(x => new AbsolutePath(x)))
        {
            var dirName = subDirPath.FileName;
            CopyDirectoryRecursive(subDirPath, target / dirName);
        }

        foreach (var filePath in source.GetFiles().Select(x => new AbsolutePath(x)))
        {
            var fileName = filePath.FileName;
            filePath.Copy(target / fileName);
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
        _temporaryPath.DeleteDirectoryRecursively();
    }

    public void Dispose()
    {
        ClearOutput();
    }
}

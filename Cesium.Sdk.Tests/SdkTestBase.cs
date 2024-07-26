using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cesium.Solution.Metadata;
using Cesium.TestFramework;
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

        var nupkgPath = Path.GetFullPath(Path.Combine(SolutionMetadata.SourceRoot, "artifacts", "package", "debug"));
        _testOutputHelper.WriteLine($"Local NuGet feed: {nupkgPath}.");
        EmitNuGetConfig(NuGetConfigPath, nupkgPath);
        EmitGlobalJson(GlobalJsonPath, $"{SolutionMetadata.VersionPrefix}");
    }

    protected async Task<BuildResult> ExecuteTargets(string projectName, params string[] targets)
    {
        var projectFile = $"{projectName}/{projectName}.ceproj";
        var joinedTargets = string.Join(";", targets);
        var testProjectFile = Path.GetFullPath(Path.Combine(_temporaryPath, projectFile));
        var testProjectFolder = Path.GetDirectoryName(testProjectFile) ?? throw new ArgumentNullException(nameof(testProjectFile));
        var binLogFile = Path.Combine(testProjectFolder, $"build_result_{projectName}_{DateTime.UtcNow:yyyy-dd-M_HH-mm-s}.binlog");

        const string objFolderPropertyName = "IntermediateOutputPath";
        const string binFolderPropertyName = "OutDir";

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = testProjectFolder,
            FileName = "dotnet",
            ArgumentList = { "msbuild", testProjectFile, $"/t:{joinedTargets}", "/restore", $"/bl:{binLogFile}" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        foreach (var (name, var) in _dotNetEnvVars)
        {
            startInfo.Environment[name] = var;
        }

        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _testOutputHelper.WriteLine($"[stdout]: {e.Data}");
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
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
            testProjectFile,
            env: _dotNetEnvVars,
            objFolderPropertyName,
            binFolderPropertyName);
        _testOutputHelper.WriteLine($"Properties request result: {JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = false })}");

        var binFolder = NormalizePath(Path.GetFullPath(properties[binFolderPropertyName], testProjectFolder));
        var objFolder = NormalizePath(Path.GetFullPath(properties[objFolderPropertyName], testProjectFolder));

        var binArtifacts = CollectArtifacts(binFolder);
        var objArtifacts = CollectArtifacts(objFolder);

        var result = new BuildResult(process.ExitCode, binArtifacts, objArtifacts);
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

    private static void EmitNuGetConfig(string configFilePath, string packageSourcePath)
    {
        File.WriteAllText(configFilePath, $"""
            <configuration>
                <packageSources>
                    <add key="local" value="{packageSourcePath}" />
               </packageSources>
            </configuration>
            """);
    }

    private static void EmitGlobalJson(string globalJsonPath, string packageVersion)
    {
        File.WriteAllText(globalJsonPath, $$"""
            {
                "msbuild-sdks": {
                    "Cesium.Sdk" : "{{packageVersion}}"
                }
            }
            """);
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

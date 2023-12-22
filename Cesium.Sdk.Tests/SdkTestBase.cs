using System.Diagnostics;
using System.Reflection;
using Cesium.Solution.Metadata;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public abstract class SdkTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _temporaryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    protected string NuGetConfigPath => Path.Combine(_temporaryPath, "NuGet.config");
    protected string GlobalJsonPath => Path.Combine(_temporaryPath, "global.json");

    public SdkTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

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

    protected BuildResult ExecuteTargets(string projectName, params string[] targets)
    {
        var projectFile = $"{projectName}/{projectName}.ceproj";
        var joinedTargets = string.Join(";", targets);
        var testProjectFile = Path.GetFullPath(Path.Combine(_temporaryPath, projectFile));
        var testProjectFolder = Path.GetDirectoryName(testProjectFile) ?? throw new ArgumentNullException(nameof(testProjectFile));
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = testProjectFolder,
            FileName = "dotnet",
            Arguments = $"build \"{testProjectFile}\" -t:{joinedTargets} -v:diag /bl:build_result.binlog",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
        };

        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _testOutputHelper.WriteLine($"[stdout]: {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _testOutputHelper.WriteLine($"[stderr]: {e.Data}");
            }
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        var success = process.ExitCode == 0;

        _testOutputHelper.WriteLine(success
            ? "Build succeeded"
            : $"Build failed with exit code {process.ExitCode}");

        var binFolder = Path.Combine(testProjectFolder, "bin");
        var objFolder = Path.Combine(testProjectFolder, "obj");

        var binArtifacts = CollectArtifacts(binFolder);
        var objArtifacts = CollectArtifacts(objFolder);

        return new BuildResult(process.ExitCode, binArtifacts, objArtifacts);

        IReadOnlyCollection<string> CollectArtifacts(string folder) =>
            Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(folder, path))
                    .ToList()
                : Array.Empty<string>();
    }

    private static void EmitNuGetConfig(string configFilePath, string packageSourcePath)
    {
        File.WriteAllText(configFilePath, $"""
            <configuration>
                <config>
                    <add key="globalPackagesFolder" value="packages" />
                </config>
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

    protected record BuildResult(
        int ExitCode,
        IReadOnlyCollection<string> OutputArtifacts,
        IReadOnlyCollection<string> IntermediateArtifacts);

    protected void ClearOutput()
    {
        Directory.Delete(_temporaryPath, true);
    }
}

using System.Runtime.InteropServices;
using Cesium.TestFramework;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public class CesiumCompileTests(ITestOutputHelper testOutputHelper) : SdkTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("SimpleCoreExe")]
    public async Task CesiumCompile_Core_Exe_ShouldSucceed(string projectName)
    {
        HashSet<string> expectedObjArtifacts =
        [
            $"{projectName}.dll"
        ];

        var hostExeFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{projectName}.exe" : projectName;
        HashSet<string> expectedBinArtifacts =
        [
            $"{projectName}.dll",
            hostExeFile,
            "Cesium.Runtime.dll",
            $"{projectName}.runtimeconfig.json",
            $"{projectName}.deps.json",
        ];

        var result = await ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertCollection.Includes(expectedObjArtifacts, result.IntermediateArtifacts.Select(a => a.FileName).ToList());
        AssertCollection.Includes(expectedBinArtifacts, result.OutputArtifacts.Select(a => a.FileName).ToList());
    }

    [Theory]
    [InlineData("SimpleNetfxExe")]
    public async Task CesiumCompile_NetFx_Exe_ShouldSucceed(string projectName)
    {
        HashSet<string> expectedObjArtifacts =
        [
            $"{projectName}.exe"
        ];

        HashSet<string> expectedBinArtifacts =
        [
            $"{projectName}.exe",
            "Cesium.Runtime.dll",
            $"{projectName}.runtimeconfig.json"
        ];

        var result = await ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertCollection.Includes(expectedObjArtifacts, result.IntermediateArtifacts.Select(a => a.FileName).ToList());
        AssertCollection.Includes(expectedBinArtifacts, result.OutputArtifacts.Select(a => a.FileName).ToList());
    }

    [Theory]
    [InlineData("SimpleCoreLibrary")]
    [InlineData("SimpleNetStandardLibrary")]
    [InlineData("SimpleCoreLibraryWithHeader")]
    public async Task CesiumCompile_Core_Library_ShouldSucceed(string projectName)
    {
        string[] expectedObjArtifacts =
        [
            $"{projectName}.dll"
        ];

        string[] expectedBinArtifacts =
        [
            $"{projectName}.dll",
            $"{projectName}.deps.json",
        ];

        var result = await ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertCollection.Includes(expectedObjArtifacts, result.IntermediateArtifacts.Select(a => a.FileName).ToList());
        AssertCollection.Includes(expectedBinArtifacts, result.OutputArtifacts.Select(a => a.FileName).ToList());
    }

    [Theory]
    [InlineData("SimpleNetfxLibrary")]
    public async Task CesiumCompile_NetFxLibrary_ShouldSucceed(string projectName)
    {
        HashSet<string> expectedObjArtifacts =
        [
            $"{projectName}.dll"
        ];

        HashSet<string> expectedBinArtifacts =
        [
            $"{projectName}.dll",
            "Cesium.Runtime.dll",
        ];

        var result = await ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertCollection.Includes(expectedObjArtifacts, result.IntermediateArtifacts.Select(a => a.FileName).ToList());
        AssertCollection.Includes(expectedBinArtifacts, result.OutputArtifacts.Select(a => a.FileName).ToList());
    }
}

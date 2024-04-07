using System.Runtime.InteropServices;
using Cesium.Sdk.Tests.Framework;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public class CesiumCompileTests(ITestOutputHelper testOutputHelper) : SdkTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("SimpleCoreExe")]
    public void CesiumCompile_Core_Exe_ShouldSucceed(string projectName)
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

        var result = ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertEx.Includes(expectedObjArtifacts, result.IntermediateArtifacts);
        AssertEx.Includes(expectedBinArtifacts, result.OutputArtifacts);
    }

    [Theory]
    [InlineData("SimpleNetfxExe")]
    public void CesiumCompile_NetFx_Exe_ShouldSucceed(string projectName)
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

        var result = ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertEx.Includes(expectedObjArtifacts, result.IntermediateArtifacts);
        AssertEx.Includes(expectedBinArtifacts, result.OutputArtifacts);
    }

    [Theory]
    [InlineData("SimpleCoreLibrary")]
    [InlineData("SimpleNetStandardLibrary")]
    public void CesiumCompile_Core_Library_ShouldSucceed(string projectName)
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

        var result = ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertEx.Includes(expectedObjArtifacts, result.IntermediateArtifacts);
        AssertEx.Includes(expectedBinArtifacts, result.OutputArtifacts);
    }

    [Theory]
    [InlineData("SimpleNetfxLibrary")]
    public void CesiumCompile_NetFxLibrary_ShouldSucceed(string projectName)
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

        var result = ExecuteTargets(projectName, "Restore", "Build");

        Assert.True(result.ExitCode == 0);
        AssertEx.Includes(expectedObjArtifacts, result.IntermediateArtifacts);
        AssertEx.Includes(expectedBinArtifacts, result.OutputArtifacts);
    }
}

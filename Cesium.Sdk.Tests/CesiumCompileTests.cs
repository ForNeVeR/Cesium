// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Cesium.TestFramework;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public class CesiumCompileTests(ITestOutputHelper testOutputHelper) : SdkTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("SimpleCoreExe")]
    [InlineData("SimpleCoreExe7")]
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
    [InlineData("SimpleNetfxExe472")]
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
    [InlineData("SimpleNetfxExe461")]
    public async Task CesiumCompile_NetFx_Exe_NotSupported(string projectName)
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

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "Unsupported TargetFramework: net461. Supported frameworks are: net6.0 and up, netstandard2.0 and net462 and up.",
            result.StdOutOutput);
        Assert.Empty(result.OutputArtifacts);
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

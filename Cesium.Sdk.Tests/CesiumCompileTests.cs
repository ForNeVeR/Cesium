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
    [InlineData("SimpleCoreExe10")]
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

        // TODO: We should extract the exact error messages in structured format.
        Assert.Contains(
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

    [Theory]
    [InlineData("SimpleCoreExe")]
    [InlineData("SimpleCoreExe7")]
    [InlineData("SimpleCoreExe10")]
    public async Task CesiumPublish_Core_Exe_ShouldSucceed(string projectName)
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

        var result = await ExecuteTargets(projectName, "Restore", "Publish");

        Assert.True(result.ExitCode == 0);
        AssertCollection.Includes(expectedObjArtifacts, result.IntermediateArtifacts.Select(a => a.FileName).ToList());
        AssertCollection.Includes(expectedBinArtifacts, result.OutputArtifacts.Select(a => a.FileName).ToList());
    }

    [Fact]
    public async Task CesiumPublish_Core_ExeWithDeps_ShouldSucceed()
    {
        HashSet<string> expectedObjArtifacts =
        [
        ];

        var hostExeFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"ConsoleApp_Net10.exe" : "ConsoleApp_Net10";
        HashSet<string> expectedBinArtifacts =
        [
            $"ConsoleApp_Net10.dll",
            hostExeFile,
            "CesiumLib.dll",
            $"ConsoleApp_Net10.runtimeconfig.json",
            $"ConsoleApp_Net10.deps.json",
        ];

        var result = await ExecuteTargets("App_WithCesiumLib/App_WithCesiumLib.slnx",
            "App_WithCesiumLib/ConsoleApp_Net10/ConsoleApp_Net10.csproj",
            "App_WithCesiumLib", "PublishDir", ["Restore", "Publish"], ["/nr:false", "/p:SelfContained=true"]);

        Assert.True(result.ExitCode == 0);
        AssertCollection.Includes(expectedObjArtifacts, result.IntermediateArtifacts.Select(a => a.FileName).ToList());
        AssertCollection.Includes(expectedBinArtifacts, result.OutputArtifacts.Select(a => a.FileName).ToList());
    }
}

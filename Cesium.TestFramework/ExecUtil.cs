// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Medallion.Shell;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.TestFramework;

public static class ExecUtil
{
    public static readonly LocalPath DotNetHost = new("dotnet");

    /// <summary>
    /// Environment passed to every process started by the tests, to prevent the .NET SDK from leaving long-lived
    /// processes (MSBuild worker nodes, the MSBuild server, the Roslyn compiler server) behind after the build.
    /// </summary>
    /// <remarks>See <a href="https://github.com/ForNeVeR/Cesium/issues/1017">#1017</a>.</remarks>
    private static readonly IReadOnlyDictionary<string, string> HygienicEnvironment = new Dictionary<string, string>
    {
        ["MSBUILDDISABLENODEREUSE"] = "1",
        ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0",
        ["UseSharedCompilation"] = "false"
    };

    public static async Task RunToSuccess(
        ITestOutputHelper? output,
        LocalPath executable,
        AbsolutePath workingDirectory,
        string[] args,
        string? inputContent = null,
        IReadOnlyDictionary<string, string>? additionalEnvironment = null)
    {
        var result = await Run(output, executable, workingDirectory, args, inputContent, additionalEnvironment);
        Assert.True(result.Success, $"The failed to run {executable} with arguments {string.Join(" ", args)}");
    }

    public static async Task<CommandResult> Run(
        ITestOutputHelper? output,
        LocalPath executable,
        AbsolutePath workingDirectory,
        string[] args,
        string? inputContent = null,
        IReadOnlyDictionary<string, string>? additionalEnvironment = null)
    {
        output?.WriteLine($"$ {executable} {string.Join(" ", args)}");
        using var command = Command.Run(executable.Value, args, o =>
        {
            o.WorkingDirectory(workingDirectory.Value);
            // Keep the Process object available for the job object assignment.
            o.DisposeOnExit(false);
            if (inputContent is { })
            {
                o.StartInfo(_ => _.RedirectStandardInput = true);
            }

            o.EnvironmentVariables(HygienicEnvironment);
            if (additionalEnvironment != null)
            {
                o.EnvironmentVariables(additionalEnvironment);
            }
        });

        // This is to guarantee the whole process subtree termination: no stale / worker MSBuild nodes, etc.
        await using (OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600)
                         ? WindowsJobObject.TryCreateFor(command.Process, output)
                         : null)
        {

            if (inputContent is { })
            {
                command.StandardInput.Write(inputContent);
                command.StandardInput.Close();
            }

            await command.Process.WaitForExitAsync();
        }

        var result = await command.Task;
        foreach (var s in result.StandardOutput.Split("\n"))
            output?.WriteLine(s.TrimEnd());
        if (result.StandardError.Trim() != "")
        {
            foreach (var s in result.StandardError.Split("\n"))
                output?.WriteLine($"[ERR] {s.TrimEnd()}");
        }

        output?.WriteLine($"Command exit code: {result.ExitCode}");
        return result;
    }
}

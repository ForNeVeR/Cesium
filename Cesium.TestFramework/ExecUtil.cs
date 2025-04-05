// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Medallion.Shell;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.TestFramework;

public static class ExecUtil
{
    public static readonly LocalPath DotNetHost = new("dotnet");

    public static async Task RunToSuccess(
        ITestOutputHelper? output,
        LocalPath executable,
        AbsolutePath workingDirectory,
        string[] args,
        IReadOnlyDictionary<string, string>? additionalEnvironment = null)
    {
        var result = await Run(output, executable, workingDirectory, args, additionalEnvironment);
        Assert.True(result.Success);
    }

    public static async Task<CommandResult> Run(
        ITestOutputHelper? output,
        LocalPath executable,
        AbsolutePath workingDirectory,
        string[] args,
        IReadOnlyDictionary<string, string>? additionalEnvironment = null)
    {
        output?.WriteLine($"$ {executable} {string.Join(" ", args)}");
        var result = await Command.Run(executable.Value, args, o =>
        {
            o.WorkingDirectory(workingDirectory.Value);
            if (additionalEnvironment != null)
            {
                foreach (var (key, value) in additionalEnvironment)
                {
                    o.EnvironmentVariable(key, value);
                }
            }
        }).Task;
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

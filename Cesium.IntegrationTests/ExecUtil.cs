using Medallion.Shell;
using Xunit;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

internal static class ExecUtil
{
    public static void RunToSuccess(
        ITestOutputHelper? output,
        string executable,
        string workingDirectory,
        string[] args)
    {
        var result = Run(output, executable, workingDirectory, args);
        Assert.True(result.Success);
    }

    public static CommandResult Run(
        ITestOutputHelper? output,
        string executable,
        string workingDirectory,
        string[] args)
    {
        output?.WriteLine($"$ {executable} {string.Join(" ", args)}");
        var result = Command.Run(executable, args, o => o.WorkingDirectory(workingDirectory)).Result;
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

using Medallion.Shell;
using Xunit.Abstractions;

namespace Cesium.Test.Framework;

/// <summary>Utils to properly run commands like <c>dotnet build</c> and such.</summary>
public static class DotNetCliHelper
{
    public static async Task ShutdownBuildServer()
    {
        await RunToSuccess(null, "dotnet", Environment.CurrentDirectory, new[]
        {
            "build-server",
            "shutdown"
        });
    }

    public static async Task BuildDotNetProject(ITestOutputHelper output, string configuration, string projectFilePath)
    {
        await RunToSuccess(output, "dotnet", Path.GetDirectoryName(projectFilePath)!, new[]
        {
            "build",
            "--configuration", configuration,
            projectFilePath
        });
    }

    public static Task<CommandResult> RunDotNetDll(
        ITestOutputHelper output,
        string workingDirectoryPath,
        string dllPath) =>
        ExecUtil.Run(output, "dotnet", workingDirectoryPath, new[] { dllPath });

    public static Task RunToSuccess(
        ITestOutputHelper? output,
        string executable,
        string workingDirectory,
        string[] args) => ExecUtil.RunToSuccess(output, executable, workingDirectory, args,
        new Dictionary<string, string>
        {
            // Work around https://github.com/dotnet/sdk/issues/34653
            ["MSBUILDENSURESTDOUTFORTASKPROCESSES"] = "0"
        });
}

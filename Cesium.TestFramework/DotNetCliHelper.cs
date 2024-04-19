using System.Text.Json;
using Medallion.Shell;
using Xunit.Abstractions;

namespace Cesium.TestFramework;

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

    public static async Task<string> EvaluateMSBuildProperty(ITestOutputHelper output, string projectPath, string propertyName)
    {
        var result = await ExecUtil.Run(output, "dotnet", Environment.CurrentDirectory, [ "msbuild", $"\"{projectPath}\"", $"-getProperty:{propertyName}" ]);
        return result.StandardOutput;
    }

    public static async Task<IReadOnlyDictionary<string, string>> EvaluateMSBuildProperties(ITestOutputHelper output, string projectPath, params string[] propertyNames)
    {
        if (!propertyNames.Any())
            return new Dictionary<string, string>();

        var result = await ExecUtil.Run(output, "dotnet", Environment.CurrentDirectory, [ "msbuild", $"\"{projectPath}\"", $"-getProperty:{string.Join(",", propertyNames)}" ]);
        var resultString = result.StandardOutput;
        if (propertyNames.Length == 1)
            return new Dictionary<string, string> { { propertyNames[0], resultString } };

        var resultJson = JsonDocument.Parse(resultString);
        var propertiesJson = resultJson.RootElement.GetProperty("Properties").EnumerateObject().ToArray();

        return propertiesJson
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
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

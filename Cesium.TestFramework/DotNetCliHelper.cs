// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Text.Json;
using Medallion.Shell;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.TestFramework;

/// <summary>Utils to properly run commands like <c>dotnet build</c> and such.</summary>
public static class DotNetCliHelper
{
    public static async Task ShutdownBuildServer()
    {
        await RunToSuccess(null, ExecUtil.DotNetHost, AbsolutePath.CurrentWorkingDirectory, [
            "build-server",
            "shutdown"
        ]);
    }

    public static async Task BuildDotNetProject(ITestOutputHelper output, string configuration, AbsolutePath projectFile)
    {
        await RunToSuccess(output, ExecUtil.DotNetHost, projectFile.Parent!.Value, [
            "build",
            "--configuration", configuration,
            projectFile.Value
        ]);
    }

    public static async Task<IReadOnlyDictionary<string, string>> EvaluateMSBuildProperties(
        ITestOutputHelper output,
        string projectPath,
        IReadOnlyDictionary<string, string>? env = null,
        params string[] propertyNames)
    {
        if (!propertyNames.Any())
            return new Dictionary<string, string>();

        var result = await ExecUtil.Run(
            output,
            ExecUtil.DotNetHost,
            AbsolutePath.CurrentWorkingDirectory,
            [ "msbuild", $"\"{projectPath}\"", $"-getProperty:{string.Join(",", propertyNames)}" ],
            null,
            additionalEnvironment: env);
        var resultString = result.StandardOutput;
        if (propertyNames.Length == 1)
            return new Dictionary<string, string> { { propertyNames[0], resultString } };

        var resultJson = JsonDocument.Parse(resultString);
        var propertiesJson = resultJson.RootElement.GetProperty("Properties").EnumerateObject().ToArray();

        return propertiesJson
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
    }

    public static async Task<IEnumerable<(string identity, string? fullPath)>> EvaluateMSBuildItem(
        ITestOutputHelper output,
        string projectPath,
        string itemName,
        IReadOnlyDictionary<string, string>? env = null)
    {
        var result = await ExecUtil.Run(
            output,
            ExecUtil.DotNetHost,
            AbsolutePath.CurrentWorkingDirectory,
            [ "msbuild", $"\"{projectPath}\"", $"-getItem:{itemName}" ],
            null,
            additionalEnvironment: env);
        var resultString = result.StandardOutput;
        var resultJson = JsonDocument.Parse(resultString);
        var itemsJson = resultJson.RootElement.GetProperty("Items").EnumerateObject().ToArray();
        var itemsDict = itemsJson.ToDictionary(item => item.Name, item => item.Value.EnumerateArray());

        return itemsDict[itemName].Select(meta => (meta.GetProperty("Identity").GetString()!, meta.GetProperty("FullPath").GetString()));
    }

    public static Task<CommandResult> RunDotNetDll(
        ITestOutputHelper output,
        AbsolutePath workingDirectoryPath,
        AbsolutePath dllPath,
        string? inputContent) =>
        ExecUtil.Run(output, ExecUtil.DotNetHost, workingDirectoryPath, [ dllPath.Value ], inputContent);

    public static Task RunToSuccess(
        ITestOutputHelper? output,
        LocalPath executable,
        AbsolutePath workingDirectory,
        string[] args) => ExecUtil.RunToSuccess(output, executable, workingDirectory, args,
        null,
        new Dictionary<string, string>
        {
            // Work around https://github.com/dotnet/sdk/issues/34653
            ["MSBUILDENSURESTDOUTFORTASKPROCESSES"] = "0"
        });
}

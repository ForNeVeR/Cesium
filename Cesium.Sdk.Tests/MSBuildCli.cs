using System.Text.Json;
using Medallion.Shell;

namespace Cesium.Sdk.Tests;

public static class MSBuildCli
{
    public static string EvaluateProperty(string projectPath, string propertyName)
    {
        var command = Command.Run("dotnet", "msbuild", $"\"{projectPath}\"", $"-getProperty:{propertyName}");
        command.Wait();
        return command.Result.StandardOutput;
    }

    public static IReadOnlyDictionary<string, string> EvaluateProperties(string projectPath, params string[] propertyNames)
    {
        if (!propertyNames.Any())
            return new Dictionary<string, string>();

        var command = Command.Run("dotnet", "msbuild", $"\"{projectPath}\"", $"-getProperty:{string.Join(",", propertyNames)}");
        command.Wait();
        var resultString = command.Result.StandardOutput;
        if (propertyNames.Length == 1)
            return new Dictionary<string, string> { { propertyNames[0], resultString } };

        var resultJson = JsonDocument.Parse(resultString);
        var propertiesJson = resultJson.RootElement.GetProperty("Properties").EnumerateObject().ToArray();

        return propertiesJson
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
    }
}

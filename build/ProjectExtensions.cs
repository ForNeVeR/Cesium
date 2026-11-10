// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

public static class ProjectExtensions
{
    public static IReadOnlyDictionary<string, string> GetEvaluatedProperties(
        this Project project, string configuration, params string[] propertyNames)
    {
        if (propertyNames.Length == 0)
            return new Dictionary<string, string>();

        // Evaluate with the selected SDK's MSBuild and NuGet assemblies, rather than loading them into NUKE.
        var output = DotNet(
                $"msbuild \"{project.Path}\" -nologo -property:Configuration={configuration} " +
                $"-getProperty:{string.Join(",", propertyNames)}",
                logOutput: false)
            .StdToText();

        if (propertyNames.Length == 1)
            return new Dictionary<string, string> { [propertyNames[0]] = output.Trim() };

        using var document = JsonDocument.Parse(output);
        return document.RootElement.GetProperty("Properties").EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString()!);
    }
}

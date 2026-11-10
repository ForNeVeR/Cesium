// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using ProjectProperties = System.Collections.Generic.IReadOnlyDictionary<string, string>;

public static class ProjectExtensions
{
    public static ProjectProperties GetEvaluatedProperties(this Project project, string configuration)
    {
        // Use the selected SDK's MSBuild and NuGet assemblies instead of loading them into NUKE.
        string[] propertyNames =
        [
            "MSBuildProjectFullPath",
            "VersionPrefix",
            "PackageOutputPath",
            "ArtifactsPath",
            "ArtifactsPublishOutputName",
            "RuntimeIdentifiers",
            "PublishDir"
        ];
        var properties = string.Join(",", propertyNames);
        var output = DotNet(
                $"msbuild {project.Path} -nologo -property:Configuration={configuration} -getProperty:{properties}",
                logOutput: false)
            .StdToText();

        using var document = JsonDocument.Parse(output);
        return document.RootElement.GetProperty("Properties").EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString()!);
    }

    public static IReadOnlyCollection<string> GetRuntimeIds(this ProjectProperties project)
    {
        return project.GetEvaluatedProperty("RuntimeIdentifiers").Split(";");
    }

    public static string GetVersion(this ProjectProperties project)
    {
        return project.GetEvaluatedProperty("VersionPrefix");
    }

    public static string GetPackageOutputPath(this ProjectProperties project)
    {
        return project.GetEvaluatedProperty("PackageOutputPath");
    }

    public static string GetPublishDirectory(this ProjectProperties project)
    {
        return project.GetEvaluatedProperty("PublishDir");
    }

    public static string GetEvaluatedProperty(this ProjectProperties project, string name)
    {
        return project[name];
    }
}

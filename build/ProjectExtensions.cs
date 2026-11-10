// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Project = System.Collections.Generic.IReadOnlyDictionary<string, string>;

public static class ProjectExtensions
{
    public static Project GetEvaluatedProperties(this Nuke.Common.ProjectModel.Project project, string configuration)
    {
        // Use the selected SDK's MSBuild and NuGet assemblies instead of loading them into NUKE.
        const string propertyNames = "MSBuildProjectFullPath,VersionPrefix,PackageOutputPath,ArtifactsPath," +
                                     "ArtifactsPublishOutputName,RuntimeIdentifiers,PublishDir";
        var output = DotNet(
                $"msbuild {project.Path} -nologo -property:Configuration={configuration} -getProperty:{propertyNames}",
                logOutput: false)
            .StdToText();

        using var document = JsonDocument.Parse(output);
        return document.RootElement.GetProperty("Properties").EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString()!);
    }

    public static IReadOnlyCollection<string> GetRuntimeIds(this Project project)
    {
        return project.GetEvaluatedProperty("RuntimeIdentifiers").Split(";");
    }

    public static string GetVersion(this Project project)
    {
        return project.GetEvaluatedProperty("VersionPrefix");
    }

    public static string GetPackageOutputPath(this Project project)
    {
        return project.GetEvaluatedProperty("PackageOutputPath");
    }

    public static string GetPublishDirectory(this Project project)
    {
        return project.GetEvaluatedProperty("PublishDir");
    }

    public static string GetEvaluatedProperty(this Project project, string name)
    {
        return project[name];
    }
}

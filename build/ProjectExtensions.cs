using System.Collections.Generic;
using Microsoft.Build.Evaluation;

public static class ProjectExtensions
{
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
        return project.GetProperty(name).EvaluatedValue;
    }
}

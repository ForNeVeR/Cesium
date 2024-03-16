using System.Collections.Generic;
using Microsoft.Build.Evaluation;

public static class ProjectExtensions
{
    public static IReadOnlyCollection<string> GetRuntimeIds(this Project project)
    {
        return project.GetProperty("RuntimeIdentifiers").EvaluatedValue.Split(";");
    }

    public static string GetVersion(this Project project)
    {
        return project.GetProperty("VersionPrefix").EvaluatedValue;
    }

    public static string GetPackageOutputPath(this Project project)
    {
        return project.GetProperty("PackageOutputPath").EvaluatedValue;
    }

    public static string GetPublishDirectory(this Project project)
    {
        return project.GetProperty("PublishDir").EvaluatedValue;
    }
}

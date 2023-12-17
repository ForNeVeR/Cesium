using System.Reflection;

namespace Cesium.Solution.Metadata;

public static class SolutionMetadata
{
    public static string SourceRoot =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<SolutionMetadataAttribute>()?.SourceRoot
        ?? throw new Exception($"Missing {nameof(SolutionMetadataAttribute)} metadata attribute.");

    public static string VersionPrefix =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<SolutionMetadataAttribute>()?.VersionPrefix
        ?? throw new Exception($"Missing {nameof(SolutionMetadataAttribute)} metadata attribute.");
}

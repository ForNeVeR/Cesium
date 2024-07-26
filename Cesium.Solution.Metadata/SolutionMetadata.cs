using System.Reflection;

namespace Cesium.Solution.Metadata;

public static class SolutionMetadata
{
    public static string SourceRoot => ResolvedAttribute.SourceRoot;
    public static string ArtifactsRoot => ResolvedAttribute.ArtifactsRoot;
    public static string VersionPrefix => ResolvedAttribute.VersionPrefix;

    private static SolutionMetadataAttribute ResolvedAttribute =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<SolutionMetadataAttribute>()
        ?? throw new Exception($"Missing {nameof(SolutionMetadataAttribute)} metadata attribute.");
}

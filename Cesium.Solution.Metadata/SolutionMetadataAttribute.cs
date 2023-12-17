namespace Cesium.Solution.Metadata;

public class SolutionMetadataAttribute : Attribute
{
    public string SourceRoot { get; }
    public string VersionPrefix { get; }

    public SolutionMetadataAttribute(string sourceRoot, string versionPrefix)
    {
        SourceRoot = sourceRoot;
        VersionPrefix = versionPrefix;
    }
}

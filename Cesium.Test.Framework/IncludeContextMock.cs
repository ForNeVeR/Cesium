using Cesium.Preprocessor;

namespace Cesium.Test.Framework;

public class IncludeContextMock : IIncludeContext
{
    private readonly IReadOnlyDictionary<string, string> _angleBracedFiles;
    private readonly List<string> _guardedIncludedFiles = new();

    public IncludeContextMock(IReadOnlyDictionary<string, string> angleBracedFiles)
    {
        _angleBracedFiles = angleBracedFiles;
    }

    public string LookUpAngleBracedIncludeFile(string filePath) => filePath;

    public string LookUpQuotedIncludeFile(string filePath) => filePath;

    public TextReader OpenFileStream(string filePath) => new StringReader(_angleBracedFiles[filePath]);

    public bool ShouldIncludeFile(string filePath)
    {
        return !_guardedIncludedFiles.Contains(filePath);
    }

    public void RegisterGuardedFileInclude(string filePath)
    {
        _guardedIncludedFiles.Add(filePath);
    }
}

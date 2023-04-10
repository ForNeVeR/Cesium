using Cesium.Preprocessor;

namespace Cesium.Test.Framework;

public class IncludeContextMock : IIncludeContext
{
    private readonly IReadOnlyDictionary<string, string> _angleBracedFiles;
    private readonly List<string> visitedFiles = new();

    public IncludeContextMock(IReadOnlyDictionary<string, string> angleBracedFiles)
    {
        _angleBracedFiles = angleBracedFiles;
    }

    public string LookUpAngleBracedIncludeFile(string filePath) => filePath;

    public string LookUpQuotedIncludeFile(string filePath) => filePath;

    public TextReader OpenFileStream(string filePath) => new StringReader(_angleBracedFiles[filePath]);

    public bool CanIncludeFile(string filePath)
    {
        return !visitedFiles.Contains(filePath);
    }

    public void RegisterPragmaOnceFile(string filePath)
    {
        visitedFiles.Add(filePath);
    }
}

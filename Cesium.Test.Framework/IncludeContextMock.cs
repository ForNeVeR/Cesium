using Cesium.Preprocessor;

namespace Cesium.Test.Framework;

public class IncludeContextMock : IIncludeContext
{
    private readonly IReadOnlyDictionary<string, string> _angleBracedFiles;

    public IncludeContextMock(IReadOnlyDictionary<string, string> angleBracedFiles)
    {
        _angleBracedFiles = angleBracedFiles;
    }

    public ValueTask<TextReader> LookUpAngleBracedIncludeFile(string filePath)
    {
        return ValueTask.FromResult<TextReader>(new StringReader(_angleBracedFiles[filePath]));
    }

    public ValueTask<TextReader> LookUpQuotedIncludeFile(string filePath)
    {
        throw new NotSupportedException();
    }
}

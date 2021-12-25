namespace Cesium.Preprocessor;

public interface IIncludeContext
{
    public ValueTask<TextReader> LookUpAngleBracedIncludeFile(string filePath);
    public ValueTask<TextReader> LookUpQuotedIncludeFile(string filePath);
}

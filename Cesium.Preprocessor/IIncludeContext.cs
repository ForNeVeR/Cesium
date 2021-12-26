namespace Cesium.Preprocessor;

public interface IIncludeContext
{
    ValueTask<TextReader> LookUpAngleBracedIncludeFile(string filePath);
    ValueTask<TextReader> LookUpQuotedIncludeFile(string filePath);
}

namespace Cesium.Preprocessor;

public interface IIncludeContext
{
    bool CanIncludeFile(string filePath);
    void RegisterPragmaOnceFile(string filePath);
    string LookUpAngleBracedIncludeFile(string filePath);
    string LookUpQuotedIncludeFile(string filePath);
    TextReader OpenFileStream(string filePath);
}

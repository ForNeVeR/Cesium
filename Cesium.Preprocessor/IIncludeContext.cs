namespace Cesium.Preprocessor;

public interface IIncludeContext
{
    bool ShouldIncludeFile(string filePath);
    void RegisterGuardedFileInclude(string filePath);
    string LookUpAngleBracedIncludeFile(string filePath);
    string LookUpQuotedIncludeFile(string filePath);
    TextReader OpenFileStream(string filePath);
}

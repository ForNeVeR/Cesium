using Cesium.Preprocessor;

namespace Cesium.Compiler;

public class FileSystemIncludeContext : IIncludeContext
{
    private readonly string _stdLibDirectory;
    private readonly string _currentDirectory;

    public FileSystemIncludeContext(string stdLibDirectory, string currentDirectory)
    {
        _stdLibDirectory = stdLibDirectory;
        _currentDirectory = currentDirectory;
    }

    public ValueTask<TextReader> LookUpAngleBracedIncludeFile(string filePath) => new(Task.Run(
        () =>
        {
            var path = Path.Combine(_stdLibDirectory, filePath);
            return (TextReader)new StreamReader(path);
        }));

    public ValueTask<TextReader> LookUpQuotedIncludeFile(string filePath) => new(Task.Run(
        () =>
        {
            var path = Path.Combine(_currentDirectory, filePath);
            if (File.Exists(path))
                return (TextReader)new StreamReader(path);

            path = Path.Combine(_stdLibDirectory, filePath);
            return (TextReader)new StreamReader(path);
        }));
}

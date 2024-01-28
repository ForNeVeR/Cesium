using System.Collections.Immutable;
using Cesium.Preprocessor;

namespace Cesium.Compiler;

public sealed class FileSystemIncludeContext : IIncludeContext
{
    private readonly string _stdLibDirectory;
    private readonly ImmutableArray<string> _userIncludeDirectories;
    private readonly List<string> _guardedIncludedFiles = new();

    public FileSystemIncludeContext(string stdLibDirectory, IEnumerable<string> currentDirectory)
    {
        _stdLibDirectory = stdLibDirectory;
        _userIncludeDirectories = currentDirectory.ToImmutableArray();
    }

    public string LookUpAngleBracedIncludeFile(string filePath)
    {
        var path = Path.Combine(_stdLibDirectory, filePath);
        if (File.Exists(path))
            return Path.GetFullPath(path);

        foreach (var userDirectory in _userIncludeDirectories)
        {
            path = Path.Combine(userDirectory, filePath);
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        return filePath;
    }

    public string LookUpQuotedIncludeFile(string filePath)
    {
        string path;
        foreach (var userDirectory in _userIncludeDirectories)
        {
            path = Path.Combine(userDirectory, filePath);
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        path = Path.Combine(_stdLibDirectory, filePath);
        return Path.GetFullPath(path);
    }

    public TextReader? OpenFileStream(string filePath) => File.Exists(filePath) ? new StreamReader(filePath) : null;

    public bool ShouldIncludeFile(string filePath)
    {
        return !_guardedIncludedFiles.Contains(filePath);
    }

    public void RegisterGuardedFileInclude(string filePath)
    {
        _guardedIncludedFiles.Add(filePath);
    }
}

using System.Collections.Immutable;
using System.Text;
using Cesium.Preprocessor;

namespace Cesium.Compiler;

public sealed class FileSystemIncludeContext(string stdLibDirectory, IEnumerable<string> currentDirectory)
    : IIncludeContext
{
    private readonly ImmutableArray<string> _userIncludeDirectories = [..currentDirectory];
    private readonly List<string> _guardedIncludedFiles = new();

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"Standard library directory: \"{stdLibDirectory}\"");
        result.Append("User include directories: [\n");
        foreach (var dir in _userIncludeDirectories)
        {
            result.Append($"\"{dir}\"\n");
        }
        result.Append("]");
        return result.ToString();
    }

    public string LookUpAngleBracedIncludeFile(string filePath)
    {
        var path = Path.Combine(stdLibDirectory, filePath);
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

        path = Path.Combine(stdLibDirectory, filePath);
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

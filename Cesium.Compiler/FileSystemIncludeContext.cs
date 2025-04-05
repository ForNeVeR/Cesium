// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text;
using Cesium.Preprocessor;
using TruePath;

namespace Cesium.Compiler;

public sealed class FileSystemIncludeContext(
    AbsolutePath stdLibDirectory,
    IEnumerable<AbsolutePath> currentDirectory) : IIncludeContext
{
    private readonly ImmutableArray<AbsolutePath> _userIncludeDirectories = [..currentDirectory];
    private readonly List<AbsolutePath> _guardedIncludedFiles = new();

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"Standard library directory: \"{stdLibDirectory.Value}\"");
        result.Append("User include directories: [\n");
        foreach (var dir in _userIncludeDirectories)
        {
            result.Append($"\"{dir.Value}\"\n");
        }
        result.Append("]");
        return result.ToString();
    }

    public AbsolutePath LookUpAngleBracedIncludeFile(LocalPath filePath)
    {
        var path = stdLibDirectory / filePath;
        if (path.ReadKind() != null)
            return path.Canonicalize();

        foreach (var userDirectory in _userIncludeDirectories)
        {
            path = userDirectory / filePath;
            if (path.ReadKind() != null)
                return path.Canonicalize();
        }

        return filePath.ResolveToCurrentDirectory();
    }

    public AbsolutePath LookUpQuotedIncludeFile(LocalPath file)
    {
        AbsolutePath path;
        foreach (var userDirectory in _userIncludeDirectories)
        {
            path = userDirectory / file;
            if (path.ReadKind() != null)
                return path.Canonicalize();
        }

        path = stdLibDirectory / file;
        return path.Canonicalize();
    }

    public TextReader? OpenFileStream(AbsolutePath file) =>
        file.ReadKind() != null ? new StreamReader(file.Value) : null;

    public bool ShouldIncludeFile(AbsolutePath filePath)
    {
        return !_guardedIncludedFiles.Contains(filePath.Canonicalize());
    }

    public void RegisterGuardedFileInclude(AbsolutePath filePath)
    {
        _guardedIncludedFiles.Add(filePath);
    }
}

// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Preprocessor;
using TruePath;

namespace Cesium.TestFramework;

public class IncludeContextMock(IReadOnlyDictionary<LocalPath, string> angleBracedFiles) : IIncludeContext
{
    private readonly List<AbsolutePath> _guardedIncludedFiles = new();

    public AbsolutePath LookUpAngleBracedIncludeFile(LocalPath file) => file.ResolveToCurrentDirectory();

    public AbsolutePath LookUpQuotedIncludeFile(LocalPath file) => file.ResolveToCurrentDirectory();

    public TextReader? OpenFileStream(AbsolutePath file) =>
        angleBracedFiles.TryGetValue(
            file.RelativeTo(AbsolutePath.CurrentWorkingDirectory),
            out var content)
            ? new StringReader(content)
            : null;

    public bool ShouldIncludeFile(AbsolutePath filePath) => !_guardedIncludedFiles.Contains(filePath);
    public void RegisterGuardedFileInclude(AbsolutePath filePath) => _guardedIncludedFiles.Add(filePath);
}

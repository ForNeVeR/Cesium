// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace Cesium.Preprocessor;

public interface IIncludeContext
{
    bool ShouldIncludeFile(AbsolutePath filePath);
    void RegisterGuardedFileInclude(AbsolutePath filePath);
    AbsolutePath LookUpAngleBracedIncludeFile(LocalPath file);
    AbsolutePath LookUpQuotedIncludeFile(LocalPath file);
    /// <returns><c>null</c> if the target file doesn't exist.</returns>
    TextReader? OpenFileStream(AbsolutePath file);
}

// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.IO;
using System.Runtime.InteropServices;

namespace Cesium.Sdk;

internal static class FileSystemUtil
{
    [DllImport("libc", EntryPoint = "access", SetLastError = true)]
    private static extern int Access(string path, int mode);

    private const int X_OK = 1;

    public static bool IsUnixFileExecutable(string path)
    {
        if (Directory.Exists(path)) return false;
        return Access(Path.GetFullPath(path), X_OK) == 0;
    }
}

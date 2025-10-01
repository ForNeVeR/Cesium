// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT


using System;
using System.Runtime.InteropServices;

namespace Cesium.Sdk;

internal enum Error
{
    ENOENT  = 0x1002D, // No such file or directory.
    ENOTDIR = 0x10039  // Not a directory.
}

[StructLayout(LayoutKind.Sequential)]
internal struct FileStatus
{
    internal FileStatusFlags Flags;
    internal int Mode;
    internal uint Uid;
    internal uint Gid;
    internal long Size;
    internal long ATime;
    internal long ATimeNsec;
    internal long MTime;
    internal long MTimeNsec;
    internal long CTime;
    internal long CTimeNsec;
    internal long BirthTime;
    internal long BirthTimeNsec;
    internal long Dev;
    internal long Ino;
    internal uint UserFlags;
}

internal static class FileTypes
{
    internal const int S_IFMT = 0xF000;
    internal const int S_IFIFO = 0x1000;
    internal const int S_IFCHR = 0x2000;
    internal const int S_IFDIR = 0x4000;
    internal const int S_IFREG = 0x8000;
    internal const int S_IFLNK = 0xA000;
    internal const int S_IFSOCK = 0xC000;
}

[Flags]
internal enum FileStatusFlags
{
    None = 0,
    HasBirthTime = 1,
}

internal static class FileInterop
{
    internal const string SystemNative = "libSystem.Native";

    [DllImport(SystemNative, EntryPoint = "SystemNative_Stat", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int Stat(string path, out FileStatus output);

    [DllImport(SystemNative, EntryPoint = "SystemNative_LStat", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int LStat(string path, out FileStatus output);
}

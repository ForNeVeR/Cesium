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
    public ulong st_dev;
    public ulong st_ino;
    public ulong st_nlink;
    public uint st_mode;
    public uint st_uid;
    public uint st_gid;
    public ulong st_rdev;
    public long st_size;
    public long st_blksize;
    public long st_blocks;
    public long st_atime;
    public ulong st_atime_nsec;
    public long st_mtime;
    public ulong st_mtime_nsec;
    public long st_ctime;
    public ulong st_ctime_nsec;
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

internal static class FileInterop
{
    internal const string LibcLibrary = "libc";

    [DllImport(LibcLibrary, EntryPoint = "stat", SetLastError = true)]
    internal static extern int Stat(string path, out FileStatus output);

    [DllImport(LibcLibrary, EntryPoint = "lstat", SetLastError = true)]
    internal static extern int LStat(string path, out FileStatus output);
}

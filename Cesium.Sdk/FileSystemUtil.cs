// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Cesium.Sdk;

[Flags]
internal enum FilePermissions
{
    None = 0,
    OtherExecute = 1,
    OtherWrite = 2,
    OtherRead = 4,
    GroupExecute = 8,
    GroupWrite = 16,
    GroupRead = 32,
    UserExecute = 64,
    UserWrite = 128,
    UserRead = 256,
    StickyBit = 512,
    SetGroup = 1024,
    SetUser = 2048,
}

internal class UnixFileInfo
{
    private FileStatus _status;

    public UnixFileInfo(string path)
    {
        LoadFileStatus(path);
    }

    private void LoadFileStatus(string path)
    {
        Console.WriteLine("Loading file: " + path);

        int rv = FileInterop.LStat(path, out _status);

        Console.WriteLine($"LStat returned {rv}");

        if (rv < 0)
        {
            var error = Marshal.GetLastWin32Error();

            throw (Error)error switch
            {
                Error.ENOENT =>
                    new ArgumentException("No such file or directory", nameof(path)),
                Error.ENOTDIR =>
                    new ArgumentException("A component of the path is not a directory", nameof(path)),
                _ =>
                    new InvalidOperationException($"lstat failed for {path} with error {error}")
            };
        }

        int fileType = _status.Mode & FileTypes.S_IFMT;

        Console.WriteLine($"File type: {fileType}");

        if (fileType != FileTypes.S_IFLNK)
            return;
        if (FileInterop.Stat(path, out var target) >= 0)
            _status.Mode = FileTypes.S_IFLNK | (target.Mode & (int)ValidUnixFileModes);
        else
            throw new InvalidOperationException($"Stat failed for {path}");
    }

    private int FileTypeCode => _status.Mode & FileTypes.S_IFMT;

    public FilePermissions FilePermissions =>
        (FilePermissions)(_status.Mode & (int)ValidUnixFileModes);

    public bool IsDirectory => FileTypeCode == FileTypes.S_IFDIR;

    internal const FilePermissions ValidUnixFileModes =
        FilePermissions.UserRead |
        FilePermissions.UserWrite |
        FilePermissions.UserExecute |
        FilePermissions.GroupRead |
        FilePermissions.GroupWrite |
        FilePermissions.GroupExecute |
        FilePermissions.OtherRead |
        FilePermissions.OtherWrite |
        FilePermissions.OtherExecute |
        FilePermissions.StickyBit |
        FilePermissions.SetGroup |
        FilePermissions.SetUser;
}

internal static class FileSystemUtil
{
    public static FilePermissions ExecutablePermissions =>
        FilePermissions.UserExecute
        | FilePermissions.GroupExecute
        | FilePermissions.OtherExecute;

    public static bool CheckUnixFilePermissions(string path, FilePermissions permissions)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return true; // TODO[#840]: Proper executable check for MacOS

            var info = new UnixFileInfo(Path.GetFullPath(path));
            Console.WriteLine($"FilePermissions: {info.FilePermissions} ({(int)info.FilePermissions}) IsDir: {info.IsDirectory}");
            return (info.FilePermissions & permissions) != 0 && !info.IsDirectory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UnixFileInfo has thrown: {ex.Message}");
            return false;
        }
    }
}

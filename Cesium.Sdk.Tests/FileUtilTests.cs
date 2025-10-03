// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cesium.Sdk.Tests;

public class FileUtilTests
{
    private static void CreateUnixFile(string filePath, string? link = null, UnixFileMode? mode = null)
    {
        File.WriteAllText(filePath, "empty");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
        if (mode is { } m)
            File.SetUnixFileMode(filePath, m);
        if (link != null)
            File.CreateSymbolicLink(link, filePath);
    }

    private string TestFile => $"testfile-{Process.GetCurrentProcess().Id}-{Guid.NewGuid()}";
    private string TestLink => $"testlink-{Process.GetCurrentProcess().Id}-{Guid.NewGuid()}";

    [Fact]
    public void ExecutablePermissionsCheckOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var fileName = TestFile;
            CreateUnixFile(fileName, mode: UnixFileMode.UserExecute);

            Assert.True(FileSystemUtil.CheckUnixFilePermissions(fileName, FileSystemUtil.ExecutablePermissions));
        }
    }

    [Fact]
    public void ExecutableLinkPermissionsCheckOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var fileName = TestFile;
            var linkName = TestLink;
            CreateUnixFile(fileName, link: linkName, mode: UnixFileMode.UserExecute);

            Assert.True(FileSystemUtil.CheckUnixFilePermissions(linkName, FileSystemUtil.ExecutablePermissions));
        }
    }

    [Fact]
    public void NoExecutablePermissionsCheckOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var fileName = TestFile;
            CreateUnixFile(fileName, mode: UnixFileMode.UserRead | UnixFileMode.UserWrite);

            Assert.False(FileSystemUtil.CheckUnixFilePermissions(fileName, FileSystemUtil.ExecutablePermissions));
        }
    }

    [Fact]
    public void InvalidForDirOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var dir = "/etc";
            Assert.False(FileSystemUtil.CheckUnixFilePermissions(dir, FileSystemUtil.ExecutablePermissions));
        }
    }
}

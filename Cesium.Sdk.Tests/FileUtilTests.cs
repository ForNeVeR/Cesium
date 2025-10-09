// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using TruePath;
using TruePath.SystemIo;

namespace Cesium.Sdk.Tests;

public class FileUtilTests
{
    private static void CreateUnixFile(AbsolutePath file, AbsolutePath? link = null, UnixFileMode? mode = null)
    {
        file.WriteAllText("empty");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
        if (mode is { } m)
            File.SetUnixFileMode(file.Value, m);
        if (link is { } linkFile)
        {
            linkFile.Delete();
            File.CreateSymbolicLink(linkFile.Value, file.Value);
        }
    }

    private AbsolutePath TestFile = Temporary.CreateTempFile();
    private AbsolutePath TestLink = Temporary.CreateTempFile();

    [Fact]
    public void ExecutablePermissionsCheckOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var file = TestFile;
            CreateUnixFile(file, mode: UnixFileMode.UserExecute);

            Assert.True(FileSystemUtil.IsUnixFileExecutable(file.Value));
        }
    }

    [Fact]
    public void ExecutableLinkPermissionsCheckOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var file = TestFile;
            var link = TestLink;
            CreateUnixFile(file, link: link, mode: UnixFileMode.UserExecute);

            Assert.True(FileSystemUtil.IsUnixFileExecutable(link.Value));
        }
    }

    [Fact]
    public void NoExecutablePermissionsCheckOnUnix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.True(true);
        else
        {
            var file = TestFile;
            CreateUnixFile(file, mode: UnixFileMode.UserRead | UnixFileMode.UserWrite);

            Assert.False(FileSystemUtil.IsUnixFileExecutable(file.Value));
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
            Assert.False(FileSystemUtil.IsUnixFileExecutable(dir));
        }
    }
}

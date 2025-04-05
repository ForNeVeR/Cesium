// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.Versioning;
using Cesium.TestFramework;
using Microsoft.Win32;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

internal static class WindowsEnvUtil
{
    public static readonly AbsolutePath MsCorLibPath =
        new(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll");

    public static async Task<AbsolutePath> FindVcCompilerInstallationFolder(ITestOutputHelper output)
    {
        var vsWhereLocation = new AbsolutePath(
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"));
        var installationPath = (await ExecUtil.Run(
            output,
            vsWhereLocation,
            AbsolutePath.CurrentWorkingDirectory,
            [
                "-latest",
                "-format", "value",
                "-property", "installationPath",
                "-nologo",
                "-nocolor"
            ])).StandardOutput;
        if (string.IsNullOrWhiteSpace(installationPath))
        {
            installationPath = (await ExecUtil.Run(
                output,
                vsWhereLocation,
                AbsolutePath.CurrentWorkingDirectory,
                [
                    "-latest",
                    "-format", "value",
                    "-property", "installationPath",
                    "-nologo",
                    "-nocolor",
                    "-prerelease"
                ])).StandardOutput;
        }

        if (string.IsNullOrWhiteSpace(installationPath))
        {
            throw new InvalidOperationException("Visual Studio Installation location was not found");
        }

        var vcRootLocation = Path.Combine(installationPath.Trim(), "VC", "Tools", "MSVC");
        if (!Directory.Exists(vcRootLocation))
        {
            throw new InvalidOperationException($"Visual Studio Installation does not have VC++ compiler installed at {vcRootLocation}|{installationPath}");
        }

        AbsolutePath? pathToCL = null;
        foreach (var folder in Directory.EnumerateDirectories(vcRootLocation, "14.*", SearchOption.TopDirectoryOnly)
                     .Select(x => new AbsolutePath(x)))
        {
            var clPath = folder / @"bin\Hostx86\x86\cl.exe";
            if (clPath.ReadKind() == FileEntryKind.File)
                pathToCL = folder;
        }

        if (pathToCL is not {} result)
        {
            throw new InvalidOperationException(
                "Visual Studio Installation does not have VC++ compiler installed, or it is corrupted");
        }

        return result;
    }

    [SupportedOSPlatform("windows")]
    public static AbsolutePath FindWin10Sdk()
    {
        var path = FindWin10SdkHelper(Registry.LocalMachine, @"SOFTWARE\\Wow6432Node");
        path ??= FindWin10SdkHelper(Registry.CurrentUser, @"SOFTWARE\\Wow6432Node");
        path ??= FindWin10SdkHelper(Registry.LocalMachine, @"SOFTWARE");
        path ??= FindWin10SdkHelper(Registry.CurrentUser, @"SOFTWARE");
        return path ?? throw new InvalidOperationException("Win10 SDK not found");
    }

    [SupportedOSPlatform("windows")]
    public static AbsolutePath? FindWin10SdkHelper(RegistryKey hive, string searchLocation)
    {
        var value = (string?)hive.OpenSubKey($@"{searchLocation}\Microsoft\Microsoft SDKs\Windows\v10.0")?.GetValue("InstallationFolder");
        return value == null ? null : new(value);
    }

    public static AbsolutePath FindLibsFolder(AbsolutePath win10SdkPath)
    {
        AbsolutePath? win10Libs = null;
        foreach (var versionFolder in Directory.EnumerateDirectories((win10SdkPath / "Lib").Value)
                     .Select(x => new AbsolutePath(x)))
        {
            var win10LibPathCandidate = new AbsolutePath(versionFolder) / @"um\x86";
            if (win10LibPathCandidate.ReadKind() == FileEntryKind.Directory)
                win10Libs = versionFolder;
        }

        if (win10Libs is not {} result)
        {
            throw new InvalidOperationException("Windows libs files was not found");
        }

        return result;
    }

    public static AbsolutePath FindIncludeFolder(AbsolutePath win10SdkPath)
    {
        AbsolutePath? win10Libs = null;
        foreach (var versionFolder in Directory.EnumerateDirectories((win10SdkPath / "Include").Value)
                     .Select(x => new AbsolutePath(x)))
        {
            var win10LibPathCandidate = versionFolder / "um";
            if (win10LibPathCandidate.ReadKind() == FileEntryKind.Directory)
                win10Libs = versionFolder;
        }

        if (win10Libs is not {} result)
        {
            throw new InvalidOperationException("Windows libs files was not found");
        }

        return result;
    }
}

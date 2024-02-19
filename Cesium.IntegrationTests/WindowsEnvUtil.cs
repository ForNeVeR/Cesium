using System.Runtime.Versioning;
using Cesium.TestFramework;
using Microsoft.Win32;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

internal static class WindowsEnvUtil
{
    public const string MsCorLibPath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll";

    public static async Task<string> FindVCCompilerInstallationFolder(ITestOutputHelper output)
    {
        var vswhereLocation =
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");
        var installationPath = (await ExecUtil.Run(
            output,
            vswhereLocation,
            ".",
            new[]
            {
                "-latest",
                "-format", "value",
                "-property", "installationPath",
                "-nologo",
                "-nocolor"
            })).StandardOutput;
        if (string.IsNullOrWhiteSpace(installationPath))
        {
            installationPath = (await ExecUtil.Run(
                output,
                vswhereLocation,
                ".",
                new[]
                {
                    "-latest",
                    "-format", "value",
                    "-property", "installationPath",
                    "-nologo",
                    "-nocolor",
                    "-prerelease"
                })).StandardOutput;
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

        string? pathToCL = null;
        foreach (var folder in Directory.EnumerateDirectories(vcRootLocation, "14.*", SearchOption.TopDirectoryOnly))
        {
            var clPath = Path.Combine(folder, @"bin\Hostx86\x86\cl.exe");
            if (File.Exists(clPath))
                pathToCL = folder;
        }

        if (pathToCL is null)
        {
            throw new InvalidOperationException(
                "Visual Studio Installation does not have VC++ compiler installed, or it is corrupted");
        }

        return pathToCL;
    }

    [SupportedOSPlatform("windows")]
    public static string FindWin10Sdk()
    {
        string? path = FindWin10SdkHelper(Registry.LocalMachine, @"SOFTWARE\\Wow6432Node");
        path ??= FindWin10SdkHelper(Registry.CurrentUser, @"SOFTWARE\\Wow6432Node");
        path ??= FindWin10SdkHelper(Registry.LocalMachine, @"SOFTWARE");
        path ??= FindWin10SdkHelper(Registry.CurrentUser, @"SOFTWARE");
        return path ?? throw new InvalidOperationException("Win10 SDK not found");
    }

    [SupportedOSPlatform("windows")]
    public static string? FindWin10SdkHelper(RegistryKey hive, string searchLocation)
    {
        return (string?)hive.OpenSubKey($@"{searchLocation}\Microsoft\Microsoft SDKs\Windows\v10.0")?.GetValue("InstallationFolder");
    }

    public static string FindLibsFolder(string win10SdkPath)
    {
        string? win10Libs = null;
        foreach (var versionFolder in Directory.EnumerateDirectories(Path.Combine(win10SdkPath, "Lib")))
        {
            var win10LibPathCandidate = Path.Combine(versionFolder, @"um\x86");
            if (Directory.Exists(win10LibPathCandidate))
                win10Libs = versionFolder;
        }

        if (win10Libs is null)
        {
            throw new InvalidOperationException("Windows libs files was not found");
        }

        return win10Libs;
    }

    public static string FindIncludeFolder(string win10SdkPath)
    {
        string? win10Libs = null;
        foreach (var versionFolder in Directory.EnumerateDirectories(Path.Combine(win10SdkPath, "Include")))
        {
            var win10LibPathCandidate = Path.Combine(versionFolder, @"um");
            if (Directory.Exists(win10LibPathCandidate))
                win10Libs = versionFolder;
        }

        if (win10Libs is null)
        {
            throw new InvalidOperationException("Windows libs files was not found");
        }

        return win10Libs;
    }
}

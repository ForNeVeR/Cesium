// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.TestFramework.Tests;

public class ExecUtilTests(ITestOutputHelper output)
{
    [WindowsFact]
    public async Task ProcessHasTerminatedWhenExitWaitCompletes()
    {
        using var compilation = new CSharpCompilationUtil();
        var assembly = await compilation.CompileCSharpAssembly(
            output, CSharpCompilationUtil.DefaultRuntime, [],
            "System.Environment.Exit(42);",
            isApplication: true);

        for (var attempt = 0; attempt < 10; ++attempt)
        {
            using var process = Process.Start(new ProcessStartInfo("dotnet")
            {
                ArgumentList = { assembly.Value },
                UseShellExecute = false,
                CreateNoWindow = true
            })!;
            try
            {
                var stopwatch = Stopwatch.StartNew();
                uint exitCode;
                do
                {
                    if (!GetExitCodeProcess(process.SafeHandle, out exitCode))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), "The child did not exit in time.");
                    Thread.SpinWait(100);
                } while (exitCode == 259); // STILL_ACTIVE

                // Do not call HasExited here: it can raise Exited before the native handle is signaled.
                await ExecUtil.WaitForProcessExit(process);
                Assert.True(process.WaitForExit(0), "The exit wait completed before native process termination.");
                Assert.Equal(42, process.ExitCode);
            }
            finally
            {
                if (!process.WaitForExit(0)) process.Kill();
                process.WaitForExit();
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint exitCode);

    [WindowsFact]
    public async Task ProcessTreeIsTerminatedAfterCommandExit()
    {
        // cmd.exe exits right away, leaving a long-running ping.exe behind (much like cl.exe leaves vctip.exe). The
        // initial short ping makes sure cmd.exe has been put into the job object before it spawns the long-running one.
        // ping.exe also inherits the output pipes, so the command won't complete before it's terminated.
        var stopwatch = Stopwatch.StartNew();
        await ExecUtil.RunToSuccess(
            output,
            new LocalPath("cmd.exe"),
            AbsolutePath.CurrentWorkingDirectory,
            ["/c", "ping -n 2 127.0.0.1 >nul & start /b ping -n 120 127.0.0.1"]);
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(60),
            $"The command took {stopwatch.Elapsed}, so the child process was not terminated with it.");
    }
}

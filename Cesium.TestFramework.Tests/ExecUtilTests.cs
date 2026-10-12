// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.TestFramework.Tests;

public class ExecUtilTests(ITestOutputHelper output)
{
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

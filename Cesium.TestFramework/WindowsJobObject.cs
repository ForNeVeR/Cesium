// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.JobObjects;
using Xunit.Abstractions;

namespace Cesium.TestFramework;

/// <summary>
/// Windows job object that terminates a process and its descendants (such as <c>vctip.exe</c>) on disposal, to make
/// sure they don't outlive the test that started them.
/// </summary>
/// <remarks>
/// <para>
/// This is a best-effort guard: the main protection against stale processes is the environment set up by
/// <see cref="ExecUtil"/>. Any failure here gets logged to the test output and never affects the test.
/// </para>
/// <para>
/// The process gets assigned to the job after it has started, so the following processes may stay out of the job:
/// descendants spawned before the assignment, and children of a process that has exited before the assignment.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows5.1.2600")]
internal sealed class WindowsJobObject : IAsyncDisposable
{
    private const uint TerminatedProcessExitCode = 1;
    private static readonly TimeSpan TerminationPollInterval = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(10);

    private readonly SafeFileHandle _handle;
    private readonly ITestOutputHelper? _output;

    private WindowsJobObject(SafeFileHandle handle, ITestOutputHelper? output)
    {
        _handle = handle;
        _output = output;
    }

    /// <summary>Puts the process into a new job object.</summary>
    /// <returns>The job object, or <c>null</c> if the process could not be put into a job.</returns>
    public static WindowsJobObject? TryCreateFor(Process process, ITestOutputHelper? output)
    {
        var handle = PInvoke.CreateJobObject(null, null);
        if (handle.IsInvalid)
        {
            LogLastError(output, "Cannot create a job object");
            handle.Dispose();
            return null;
        }

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };
        if (!PInvoke.SetInformationJobObject(
                handle,
                JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                MemoryMarshal.AsBytes(new ReadOnlySpan<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(in info))))
        {
            LogLastError(output, "Cannot set the job object limits");
            handle.Dispose();
            return null;
        }

        if (!PInvoke.AssignProcessToJobObject(handle, process.SafeHandle))
        {
            LogLastError(output, "Cannot assign the process to a job object");
            handle.Dispose();
            return null;
        }

        return new WindowsJobObject(handle, output);
    }

    /// <summary>
    /// Terminates all the processes still running in the job, and waits for them to exit, up to a time limit.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_handle.IsClosed) return;

        try
        {
            if (PInvoke.TerminateJobObject(_handle, TerminatedProcessExitCode))
                await WaitForTermination();
            else
                LogLastError(_output, "Cannot terminate the job object");
        }
        finally
        {
            _handle.Dispose();
        }
    }

    private async Task WaitForTermination()
    {
        var stopwatch = Stopwatch.StartNew();
        while (TryGetActiveProcessCount(out var activeProcesses) && activeProcesses > 0)
        {
            if (stopwatch.Elapsed >= TerminationTimeout)
            {
                _output?.WriteLine(
                    $"[JOB] {activeProcesses} process(es) still running {TerminationTimeout} after the job termination.");
                return;
            }

            await Task.Delay(TerminationPollInterval);
        }
    }

    private bool TryGetActiveProcessCount(out uint activeProcesses)
    {
        var info = new JOBOBJECT_BASIC_ACCOUNTING_INFORMATION();
        var success = PInvoke.QueryInformationJobObject(
            _handle,
            JOBOBJECTINFOCLASS.JobObjectBasicAccountingInformation,
            MemoryMarshal.AsBytes(new Span<JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(ref info)));
        if (!success)
            LogLastError(_output, "Cannot query the job object");

        activeProcesses = info.ActiveProcesses;
        return success;
    }

    private static void LogLastError(ITestOutputHelper? output, string message) =>
        output?.WriteLine(
            $"[JOB] {message}: error {Marshal.GetLastPInvokeError()} ({Marshal.GetLastPInvokeErrorMessage()}).");
}

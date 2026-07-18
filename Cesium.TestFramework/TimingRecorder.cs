// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using TruePath;

namespace Cesium.TestFramework;

/// <summary>
/// Records timing information for integration tests and generates reports.
/// Thread-safe singleton implementation.
/// </summary>
public sealed class TimingRecorder : IDisposable
{
    private static readonly Lazy<TimingRecorder> _instance = new(() => new TimingRecorder());
    private readonly object _lock = new();
    private readonly List<TestTimingResult> _results = new();
    private readonly Stopwatch _totalStopwatch = new();
    private bool _disposed;

    /// <summary>
    /// Gets the singleton instance of the TimingRecorder.
    /// </summary>
    public static TimingRecorder Instance => _instance.Value;

    private TimingRecorder()
    {
        _totalStopwatch.Start();
    }

    /// <summary>
    /// Records a test timing result.
    /// </summary>
    public void RecordTest(TestTimingResult result)
    {
        lock (_lock)
        {
            _results.Add(result);
        }
    }

    /// <summary>
    /// Gets the current operating system name.
    /// </summary>
    public static string GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        return "Unknown";
    }

    /// <summary>
    /// Generates a timing report from all recorded results.
    /// </summary>
    public TimingReport GenerateReport()
    {
        lock (_lock)
        {
            var summary = new TimingSummary
            {
                TotalTests = _results.Count,
                TotalNativeCompileTimeMs = _results.Sum(r => r.NativeCompileTimeMs),
                TotalNativeRunTimeMs = _results.Sum(r => r.NativeRunTimeMs),
                TotalCesiumCompileTimeMs = _results.Sum(r => r.CesiumCompileTimeMs),
                TotalCesiumRunTimeMs = _results.Sum(r => r.CesiumRunTimeMs),
                TotalTimeMs = _results.Sum(r => r.TotalTimeMs),
                Os = GetOsName()
            };

            return new TimingReport
            {
                Os = GetOsName(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                Tests = new List<TestTimingResult>(_results),
                Summary = summary
            };
        }
    }

    /// <summary>
    /// Writes the timing report to a JSON file.
    /// The file is written to the current working directory with a name like:
    /// integration-test-timing-Windows-20250401T120000Z.json
    /// </summary>
    public string WriteReportToFile()
    {
        var report = GenerateReport();
        var fileName = $"integration-test-timing-{report.Os}-{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}.json";
        var filePath = AbsolutePath.CurrentWorkingDirectory / fileName;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(filePath.Value, json);

        return filePath.Value;
    }

    /// <summary>
    /// Clears all recorded results.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _results.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _totalStopwatch.Stop();
        _disposed = true;
    }
}

/// <summary>
/// Helper class to measure time for a single operation.
/// </summary>
public class OperationTimer : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly Action<long> _recordAction;
    private bool _disposed;

    public OperationTimer(Action<long> recordAction)
    {
        _recordAction = recordAction;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets the elapsed time in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Stops the timer and records the elapsed time.
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            return;

        _stopwatch.Stop();
        _recordAction(_stopwatch.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _disposed = true;
    }
}
// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using TruePath;

namespace Cesium.TestFramework;

/// <summary>
/// Represents timing information for a single test execution.
/// </summary>
public record TestTimingResult
{
    public string TestName { get; init; } = string.Empty;
    public string TargetFramework { get; init; } = string.Empty;
    public string TargetArch { get; init; } = string.Empty;
    public string[] SourceFiles { get; init; } = [];
    public string OperatingSystem { get; init; } = string.Empty;
    
    /// <summary>Time to compile with native compiler (MSVC on Windows, GCC on Linux/Mac).</summary>
    public TimeSpan? NativeCompileTime { get; init; }
    
    /// <summary>Time to compile with Cesium compiler.</summary>
    public TimeSpan? CesiumCompileTime { get; init; }
    
    /// <summary>Time to run the native executable.</summary>
    public TimeSpan? NativeExecutionTime { get; init; }
    
    /// <summary>Time to run the Cesium-compiled executable.</summary>
    public TimeSpan? CesiumExecutionTime { get; init; }
    
    /// <summary>Total test time from start to finish.</summary>
    public TimeSpan TotalTime { get; init; }
    
    /// <summary>Timestamp when the test was executed.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>Whether the test passed or failed.</summary>
    public bool Success { get; init; }
    
    /// <summary>Error message if the test failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Collects and aggregates timing results for integration tests.
/// </summary>
public static class TestTimingCollector
{
    private static readonly ConcurrentBag<TestTimingResult> _results = new();
    private static readonly object _fileLock = new();
    private static AbsolutePath? _outputDirectory;
    
    /// <summary>
    /// Initializes the collector with the output directory for timing results.
    /// </summary>
    public static void Initialize(AbsolutePath outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(outputDirectory.Value);
    }
    
    /// <summary>
    /// Records a timing result.
    /// </summary>
    public static void RecordResult(TestTimingResult result)
    {
        _results.Add(result);
    }
    
    /// <summary>
    /// Gets all recorded results.
    /// </summary>
    public static IReadOnlyList<TestTimingResult> GetResults() => _results.ToList();
    
    /// <summary>
    /// Clears all recorded results.
    /// </summary>
    public static void Clear() => _results.Clear();
    
    /// <summary>
    /// Saves all timing results to a JSON file.
    /// </summary>
    public static void SaveToJson()
    {
        if (_outputDirectory == null)
        {
            throw new InvalidOperationException("TestTimingCollector has not been initialized with an output directory.");
        }
        
        var results = _results.ToList();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"integration_test_timing_{timestamp}.json";
        var filePath = _outputDirectory / fileName;
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        var summary = new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            MachineName = Environment.MachineName,
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            TotalTime = results.Aggregate(TimeSpan.Zero, (sum, r) => sum + r.TotalTime),
            AverageNativeCompileTime = results.Where(r => r.NativeCompileTime.HasValue)
                .Select(r => r.NativeCompileTime!.Value)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Average(t => t.TotalMilliseconds),
            AverageCesiumCompileTime = results.Where(r => r.CesiumCompileTime.HasValue)
                .Select(r => r.CesiumCompileTime!.Value)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Average(t => t.TotalMilliseconds),
            AverageNativeExecutionTime = results.Where(r => r.NativeExecutionTime.HasValue)
                .Select(r => r.NativeExecutionTime!.Value)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Average(t => t.TotalMilliseconds),
            AverageCesiumExecutionTime = results.Where(r => r.CesiumExecutionTime.HasValue)
                .Select(r => r.CesiumExecutionTime!.Value)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Average(t => t.TotalMilliseconds),
            Results = results
        };
        
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(summary, jsonOptions);
            File.WriteAllText(filePath.Value, json);
        }
    }
}

/// <summary>
/// Helper class for measuring execution time of operations.
/// </summary>
public class TestTimer
{
    private readonly Stopwatch _stopwatch = new();
    private readonly string _operationName;
    
    public TestTimer(string operationName)
    {
        _operationName = operationName;
    }
    
    public void Start()
    {
        _stopwatch.Restart();
    }
    
    public TimeSpan Stop()
    {
        _stopwatch.Stop();
        return _stopwatch.Elapsed;
    }
    
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    public string OperationName => _operationName;
}
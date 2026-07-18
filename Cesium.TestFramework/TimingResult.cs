// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Cesium.TestFramework;

/// <summary>
/// Represents timing information for a single test case.
/// </summary>
public class TestTimingResult
{
    /// <summary>
    /// Name of the test.
    /// </summary>
    [JsonPropertyName("testName")]
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Source file being tested.
    /// </summary>
    [JsonPropertyName("sourceFile")]
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// Target architecture (Bit32, Bit64, Wide, Dynamic).
    /// </summary>
    [JsonPropertyName("targetArch")]
    public string? TargetArch { get; set; }

    /// <summary>
    /// Target framework (NetFramework, Net).
    /// </summary>
    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Time to compile with native compiler (cl.exe or gcc), in milliseconds.
    /// </summary>
    [JsonPropertyName("nativeCompileTimeMs")]
    public long NativeCompileTimeMs { get; set; }

    /// <summary>
    /// Time to run the native executable, in milliseconds.
    /// </summary>
    [JsonPropertyName("nativeRunTimeMs")]
    public long NativeRunTimeMs { get; set; }

    /// <summary>
    /// Time to compile with Cesium compiler, in milliseconds.
    /// </summary>
    [JsonPropertyName("cesiumCompileTimeMs")]
    public long CesiumCompileTimeMs { get; set; }

    /// <summary>
    /// Time to run the Cesium-generated executable, in milliseconds.
    /// </summary>
    [JsonPropertyName("cesiumRunTimeMs")]
    public long CesiumRunTimeMs { get; set; }

    /// <summary>
    /// Total test time in milliseconds.
    /// </summary>
    [JsonPropertyName("totalTimeMs")]
    public long TotalTimeMs { get; set; }
}

/// <summary>
/// Summary statistics for all tests.
/// </summary>
public class TimingSummary
{
    /// <summary>
    /// Total number of tests executed.
    /// </summary>
    [JsonPropertyName("totalTests")]
    public int TotalTests { get; set; }

    /// <summary>
    /// Total time spent compiling with native compiler, in milliseconds.
    /// </summary>
    [JsonPropertyName("totalNativeCompileTimeMs")]
    public long TotalNativeCompileTimeMs { get; set; }

    /// <summary>
    /// Total time spent running native executables, in milliseconds.
    /// </summary>
    [JsonPropertyName("totalNativeRunTimeMs")]
    public long TotalNativeRunTimeMs { get; set; }

    /// <summary>
    /// Total time spent compiling with Cesium compiler, in milliseconds.
    /// </summary>
    [JsonPropertyName("totalCesiumCompileTimeMs")]
    public long TotalCesiumCompileTimeMs { get; set; }

    /// <summary>
    /// Total time spent running Cesium executables, in milliseconds.
    /// </summary>
    [JsonPropertyName("totalCesiumRunTimeMs")]
    public long TotalCesiumRunTimeMs { get; set; }

    /// <summary>
    /// Total execution time for all tests, in milliseconds.
    /// </summary>
    [JsonPropertyName("totalTimeMs")]
    public long TotalTimeMs { get; set; }

    /// <summary>
    /// Operating system the tests ran on.
    /// </summary>
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;
}

/// <summary>
/// Complete timing report for a test run.
/// </summary>
public class TimingReport
{
    /// <summary>
    /// Operating system the tests ran on.
    /// </summary>
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the test run (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Individual test timing results.
    /// </summary>
    [JsonPropertyName("tests")]
    public List<TestTimingResult> Tests { get; set; } = new();

    /// <summary>
    /// Summary statistics.
    /// </summary>
    [JsonPropertyName("summary")]
    public TimingSummary Summary { get; set; } = new();
}
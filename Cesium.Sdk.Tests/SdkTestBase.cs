using System.Reflection;
using Cesium.Solution.Metadata;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public abstract class SdkTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _temporaryPath = Path.GetTempFileName();

    public SdkTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        File.Delete(_temporaryPath);

        _testOutputHelper.WriteLine($"Test projects folder: {_temporaryPath}");

        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var testDataPath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "TestProjects");
        CopyDirectoryRecursive(testDataPath, _temporaryPath);

        var nupkgPath = Path.GetFullPath(Path.Combine(SolutionMetadata.SourceRoot, "artifacts", "package", "debug"));
        EmitNuGetConfig(Path.Combine(_temporaryPath, "NuGet.config"), nupkgPath);
        EmitGlobalJson(Path.Combine(_temporaryPath, "global.json"), $"{SolutionMetadata.VersionPrefix}-dev");
    }

    protected BuildResult ExecuteTargets(string projectFile, params string[] targets)
    {
        var projectInstance = new ProjectInstance(projectFile);
        var request = new BuildRequestData(projectInstance, targets);
        var parameters = new BuildParameters
        {
            Loggers = new []{ new TestOutputLogger(_testOutputHelper) }
        };
        var result = BuildManager.DefaultBuildManager.Build(parameters, request);
        return result;
    }

    private static void EmitNuGetConfig(string configFilePath, string packageSourcePath)
    {
        File.WriteAllText(configFilePath, $@"<configuration>
    <config>
        <add key=""globalPackagesFolder"" value=""packages"" />
    </config>
    <packageSources>
        <add key=""local"" value=""{packageSourcePath}"" />
    </packageSources>
</configuration>
");
    }

    private static void EmitGlobalJson(string globalJsonPath, string packageVersion)
    {
        File.WriteAllText(globalJsonPath, $@"{{
    ""msbuild-sdks"": {{
        ""Cesium.Sdk"" : ""{packageVersion}""
    }}
}}
");
    }

    private static void CopyDirectoryRecursive(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var subDirPath in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(subDirPath);
            CopyDirectoryRecursive(subDirPath, Path.Combine(target, dirName));
        }

        foreach (var filePath in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(filePath);
            File.Copy(filePath, Path.Combine(target, fileName));
        }
    }

    private class TestOutputLogger : ILogger
    {
        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;
        public string Parameters { get; set; } = string.Empty;

        private readonly ITestOutputHelper _testOutputHelper;

        public TestOutputLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += HandleEvent;
        }

        public void Shutdown()
        {
        }

        private void HandleEvent(object sender, BuildEventArgs args)
        {
            var entry = args switch
            {
                TargetFinishedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.TargetFinished, args.Message),
                TargetStartedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.TargetStarted, args.Message),
                TaskFinishedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.TaskFinished, args.Message),
                TaskStartedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.TaskStarted, args.Message),
                BuildFinishedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.BuildFinished, args.Message),
                BuildStartedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.BuildStarted, args.Message),
                CustomBuildEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.CustomEventRaised, args.Message),
                BuildErrorEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Error, BuildLogKind.ErrorRaised, args.Message),
                BuildMessageEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.MessageRaised, args.Message),
                ProjectFinishedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.ProjectFinished, args.Message),
                ProjectStartedEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.ProjectStarted, args.Message),
                BuildStatusEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.StatusEventRaised, args.Message),
                BuildWarningEventArgs =>
                    new BuildLogEntry(BuildLogLevel.Warning, BuildLogKind.WarningRaised, args.Message),
                var other =>
                    new BuildLogEntry(BuildLogLevel.Info, BuildLogKind.AnyEventRaised, other.Message)
            };

            _testOutputHelper.WriteLine($"[{entry.Level.ToString()}]: {entry.Message}");
        }
    }

    protected enum BuildLogLevel
    {
        Error,
        Warning,
        Info,
        Verbose,
        Trace,
    }

    protected enum BuildLogKind
    {
        AnyEventRaised,
        BuildFinished,
        BuildStarted,
        CustomEventRaised,
        ErrorRaised,
        MessageRaised,
        ProjectFinished,
        ProjectStarted,
        StatusEventRaised,
        TargetFinished,
        TargetStarted,
        TaskFinished,
        TaskStarted,
        WarningRaised
    }

    protected record BuildLogEntry(BuildLogLevel Level, BuildLogKind Kind, string? Message);
}

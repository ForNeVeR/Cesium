using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Cesium.TestAdapter;

internal class CompilerVerifier
{
    private readonly string _sourceFolder;

    public CompilerVerifier(string container)
    {
        _sourceFolder = FindRootFolder(container);
    }

    public string OutDir => Path.Combine(_sourceFolder, "Cesium.IntegrationTests", "bin");
    public string ObjDir => Path.Combine(_sourceFolder, "Cesium.IntegrationTests", "obj");
    public string TestCaseDir => Path.Combine(_sourceFolder, "Cesium.IntegrationTests");
    public string TargetFramework => "Net";
    public string Configuration => "Release";
    public string SourceRoot => _sourceFolder;

    private static string FindRootFolder(string file)
    {
        var directory = Path.GetDirectoryName(file);
        if (directory is null) throw new InvalidOperationException("Cannot run from root location");
        var cesiumSolution = Path.Combine(directory, "Cesium.sln");
        while (!File.Exists(cesiumSolution))
        {
            directory = Path.GetDirectoryName(directory);
            if (directory is null) throw new InvalidOperationException("Cannot run from root location");
            cesiumSolution = Path.Combine(directory, "Cesium.sln");
        }

        return cesiumSolution;
    }

    public void VerifySourceCode(string sourceCodeFile, IMessageLogger logger)
    {
        var nativeCompilerBinOutput = $"{OutDir}/out_native.exe";
        var cesiumBinOutput = $"{OutDir}/out_cs.exe";
        var nativeCompilerRunLog = $"{OutDir}/out_native.log";
        var cesiumRunLog = $"{OutDir}/out_cs.log";

        var expectedExitCode = 42;
        if (!BuildFileWithNativeCompiler(sourceCodeFile, nativeCompilerBinOutput, logger))
        {
            throw new InvalidOperationException("Native compilation failed");
        }

        var exitCode = RunApplication(nativeCompilerBinOutput, "", nativeCompilerRunLog);
        if (exitCode != expectedExitCode)
        {
            throw new InvalidOperationException($"Binary {nativeCompilerBinOutput} returned code {exitCode}, but {expectedExitCode} was expected.");
        }

        if (!BuildFileWithCesium(sourceCodeFile, cesiumBinOutput, logger))
        {
            throw new InvalidOperationException("Cesium compilation failed");
        }

        if (TargetFramework == "NetFramework")
        {
            exitCode = RunApplication(cesiumBinOutput, "", cesiumRunLog);
        }
        else
        {
            exitCode = RunApplication("dotnet", cesiumBinOutput, cesiumRunLog);
        }

        if (exitCode != expectedExitCode)
        {
            throw new InvalidOperationException($"Binary {cesiumBinOutput} returned code {exitCode}, but {expectedExitCode} was expected.");
        }

        var nativeCompilerOutput = File.ReadAllText(nativeCompilerRunLog);
        var cesiumOutput = File.ReadAllText(cesiumRunLog);
        if (nativeCompilerOutput != cesiumOutput)
        {
            throw new InvalidOperationException($"""
                Output for {sourceCodeFile} differs between native- and Cesium-compiled programs.
                "cl.exe ({sourceCodeFile}):
                {nativeCompilerOutput}
                "Cesium ({sourceCodeFile}):
                {cesiumOutput}
                """);
        }
    }

    private static int RunApplication(string application, string arguments, string log)
    {
        var process = new Process();
        process.StartInfo.FileName = application;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
        process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;

        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            File.AppendAllLines(log, new[] { outLine.Data ?? string.Empty });
        }
    }

    private bool BuildFileWithNativeCompiler(string inputFile, string outputFile, IMessageLogger logger)
    {
        Process? process;
        if (System.OperatingSystem.IsWindows())
        {
            var objDir = ObjDir;
            var commandLine = $"cl.exe /nologo {inputFile} -D__TEST_DEFINE /Fo:{objDir} /Fe:{outputFile}";
            logger.SendMessage(TestMessageLevel.Informational, $"Compiling {inputFile} with cl.exe using command line {commandLine}.");
            process = Process.Start($"cl.exe", new[] { "/nologo", inputFile, "-D__TEST_DEFINE", $"/Fo:{objDir}", $"/Fe:{outputFile}" });
        }
        else
        {
            var commandLine = $"gcc {inputFile} -o {outputFile} -D__TEST_DEFINE";
            logger.SendMessage(TestMessageLevel.Informational, $"Compiling {inputFile} with gcc using command line {commandLine}.");
            process = Process.Start($"gcc", new[] { inputFile, "-o", outputFile, "-D__TEST_DEFINE" });
        }

        int exitCode;
        if (process is null)
        {
            exitCode = 8 /*ENOEXEC*/;
        }
        else
        {
            process.WaitForExit();
            exitCode = process.ExitCode;
        }

        if (exitCode != 0)
        {
            logger.SendMessage(TestMessageLevel.Informational, $"Error: native compiler returned exit code {exitCode}.");
            return false;
        }

        return true;
    }

    private bool BuildFileWithCesium(string inputFile, string outputFile, IMessageLogger logger)
    {
        logger.SendMessage(TestMessageLevel.Informational, $"Compiling {inputFile} with Cesium.");
        Process? process;
        if (TargetFramework == "NetFramework")
        {
            var CoreLib = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll";
            var CesiumRuntime = $"{SourceRoot}/Cesium.Runtime/bin/{Configuration}/netstandard2.0/Cesium.Runtime.dll";
            process = Process.Start($"dotnet", new[] { "run", "--no-build", "--configuration", Configuration, "--project", $"{SourceRoot}/Cesium.Compiler", "--", "--nologo", inputFile, "--out", outputFile, "-D__TEST_DEFINE", "--framework", TargetFramework, "--corelib", CoreLib, "--runtime", CesiumRuntime });
        }
        else
        {
            process = Process.Start($"dotnet", new[] { "run", "--no-build", "--configuration", Configuration, "--project", $"{SourceRoot}/Cesium.Compiler", "--", "--nologo", inputFile, "--out", outputFile, "-D__TEST_DEFINE", "--framework", TargetFramework });
        }

        int exitCode;
        if (process is null)
        {
            exitCode = 8 /*ENOEXEC*/;
        }
        else
        {
            process.WaitForExit();
            exitCode = process.ExitCode;
        }

        if (exitCode != 0)
        {
            logger.SendMessage(TestMessageLevel.Informational, $"Error: Cesium.Compiler returned exit code {exitCode}.");
            return false;
        }

        return true;
    }
}

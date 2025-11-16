// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Cesium.Solution.Metadata;
using Cesium.TestFramework;
using TruePath;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

public class IntegrationTestRunner : IClassFixture<IntegrationTestContext>, IAsyncLifetime
{
    private static readonly AbsolutePath _thisProjectSourceDirectory = SolutionMetadata.SourceRoot / "Cesium.IntegrationTests";

    private readonly ITestOutputHelper _output;
    private readonly IntegrationTestContext _context;
    public IntegrationTestRunner(IntegrationTestContext context, ITestOutputHelper output)
    {
        _context = context;
        _output = output;
    }

    public Task InitializeAsync() =>
        _context.EnsureInitialized(_output);

    public Task DisposeAsync() => Task.CompletedTask;

    public static IEnumerable<object[]> TestCaseProvider()
    {
        var cFiles = Directory.EnumerateFileSystemEntries(
            _thisProjectSourceDirectory.Value,
            "*.c",
            SearchOption.AllDirectories).Select(x => new AbsolutePath(x));
        return cFiles
            .Where(IsValidForCommonTestRun)
            .Select(file => file.RelativeTo(_thisProjectSourceDirectory))
            .SelectMany(path => new[] { new object[] { TargetArch.Bit32, path.Value }, [TargetArch.Bit64, path.Value], [TargetArch.Wide, path.Value], [TargetArch.Dynamic, path.Value] });
    }

    private static bool IsValidForCommonTestRun(AbsolutePath file)
    {
        if (file.Parent?.FileName == "multi-file") return false;
        var fileName = file.FileName;
        return !fileName.EndsWith(".ignore.c")
               && !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && fileName.EndsWith(".msvc_ignore.c"));
    }

    private enum TargetFramework
    {
        NetFramework,
        Net
    }

    public enum TargetArch
    {
        Bit32,
        Bit64,
        Wide,
        Dynamic
    }

    [Theory]
    [MemberData(nameof(TestCaseProvider))]
    public async Task TestNetFramework(TargetArch arch, string relativeSourcePath)
    {
        if (OperatingSystem.IsWindows())
        {
            await _context.WrapTestBody(() => DoTest(TargetFramework.NetFramework, arch, new LocalPath(relativeSourcePath)));
        }
    }

    [Theory]
    [MemberData(nameof(TestCaseProvider))]
    public Task TestNet(TargetArch arch, string relativeSourcePath) =>
        _context.WrapTestBody(() => DoTest(TargetFramework.Net, arch, new LocalPath(relativeSourcePath)));

    [Fact]
    public Task MultiFileApplicationCompiles() =>
        _context.WrapTestBody(() => DoTest(
            TargetFramework.Net, TargetArch.Dynamic, new("multi-file/program.c"), new("multi-file/function.c")));

    [Fact]
    public Task CompilerAcceptsAnObjFileAsInput() =>
        _context.WrapTestBody(async () =>
        {
            var outRoot = Temporary.CreateTempFolder();
            try
            {
                var binDir = outRoot / "bin";
                var objDir = outRoot / "obj";
                Directory.CreateDirectory(binDir.Value);
                Directory.CreateDirectory(objDir.Value);

                var functionSource = _thisProjectSourceDirectory / "multi-file/function.c";
                var programSource = _thisProjectSourceDirectory / "multi-file/program.c";

                var nativeResult = await CompileAndRunWithNative(binDir, objDir, outRoot, [functionSource, programSource], null);

                var functionObject = await GenerateJsonObjectFile(objDir, functionSource, TargetFramework.Net);
                var programObject = await GenerateJsonObjectFile(objDir, programSource, TargetFramework.Net);

                var cesiumResult = await CompileAndRunWithCesium(
                    binDir,
                    objDir,
                    outRoot,
                    TargetFramework.Net,
                    TargetArch.Dynamic,
                    [functionObject, programObject],
                    null);

                Assert.Equal(nativeResult.ReplaceLineEndings("\n"), cesiumResult.ReplaceLineEndings("\n"));
            }
            finally
            {
                Directory.Delete(outRoot.Value, recursive: true);
            }
        });

    private async Task DoTest(TargetFramework targetFramework, TargetArch arch, params LocalPath[] relativeSourcePaths)
    {
        var outRoot = Temporary.CreateTempFolder();
        try
        {
            var paths = "[" + string.Join(", ", relativeSourcePaths.Select(x => $"\"{x.Value}\"")) + "]";
            _output.WriteLine($"Building source files {paths} in directory \"{outRoot}\".");

            string? inputContent = null;
            if (relativeSourcePaths.Length > 0)
            {
                var inputPath = SolutionMetadata.SourceRoot / "Cesium.IntegrationTests" / relativeSourcePaths[0].WithExtension(".in");
                if (File.Exists(inputPath.ToString()))
                {
                    inputContent = File.ReadAllText(inputPath.ToString());
                }
            }
            var binDir = outRoot / "bin";
            var objDir = outRoot / "obj";
            Directory.CreateDirectory(binDir.Value);
            Directory.CreateDirectory(objDir.Value);

            var sourceFiles = relativeSourcePaths
                .Select(x => SolutionMetadata.SourceRoot / "Cesium.IntegrationTests" / x)
                .ToList();

            await CompileAndRunWithNative(binDir, objDir, outRoot, sourceFiles, inputContent);
            await CompileAndRunWithCesium(binDir, objDir, outRoot, targetFramework, arch, sourceFiles, inputContent);
        }
        finally
        {
            Directory.Delete(outRoot.Value, recursive: true);
        }
    }

    private async Task<string> CompileAndRunWithNative(
        AbsolutePath binDir,
        AbsolutePath objDir,
        AbsolutePath outRoot,
        IList<AbsolutePath> sources,
        string? inputContent)
    {
        var nativeExecutable = await BuildExecutableWithNativeCompiler(binDir, objDir, sources);
        var nativeResult = await ExecUtil.Run(_output, nativeExecutable, outRoot, [], inputContent);
        Assert.Equal(42, nativeResult.ExitCode);
        Assert.Empty(nativeResult.StandardError);
        return nativeResult.StandardOutput;
    }

    private async Task<string> CompileAndRunWithCesium(
        AbsolutePath binDir,
        AbsolutePath objDir,
        AbsolutePath outRoot,
        TargetFramework targetFramework,
        TargetArch arch,
        IList<AbsolutePath> inputFiles,
        string? inputContent)
    {
        var managedExecutable = await BuildExecutableWithCesium(
            binDir,
            objDir,
            inputFiles,
            targetFramework,
            arch);
        var managedResult = await (targetFramework switch
        {
            TargetFramework.Net => DotNetCliHelper.RunDotNetDll(_output, outRoot, managedExecutable, inputContent),
            TargetFramework.NetFramework => ExecUtil.Run(_output, managedExecutable, outRoot, [], inputContent),
            _ => throw new ArgumentOutOfRangeException(nameof(targetFramework), targetFramework, null)
        });
        Assert.Equal(42, managedResult.ExitCode);
        Assert.Empty(managedResult.StandardError);
        return managedResult.StandardOutput;
    }

    private static readonly object _tempDirCreator = new();
    private static string CreateTempDir()
    {
        lock (_tempDirCreator)
        {
            var path = Path.GetTempFileName();
            File.Delete(path);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    private async Task<AbsolutePath> BuildExecutableWithNativeCompiler(
        AbsolutePath binDir,
        AbsolutePath objDir,
        IList<AbsolutePath> sourceFiles)
    {
        var executableFile = binDir / "out_native.exe";
        if (OperatingSystem.IsWindows())
        {
            var paths = "[" + string.Join(", ", sourceFiles.Select(x => $"\"{x.Value}\"")) + "]";
            _output.WriteLine($"Compiling {paths} with cl.exe.");

            var vcInstallationFolder = _context.VisualStudioPath;
            Assert.True(vcInstallationFolder.HasValue);

            var clExePath = vcInstallationFolder.Value / @"bin\Hostx86\x86\cl.exe";
            var pathToLibs = vcInstallationFolder.Value / @"lib\x86";
            var pathToIncludes = vcInstallationFolder.Value / "include";
            var win10SdkPath = WindowsEnvUtil.FindWin10Sdk();
            var win10Libs = WindowsEnvUtil.FindLibsFolder(win10SdkPath);
            var win10Include = WindowsEnvUtil.FindIncludeFolder(win10SdkPath);
            await ExecUtil.RunToSuccess(
                _output,
                clExePath,
                objDir,
                [
                    "/nologo",
                    ..sourceFiles.Select(x => x.Value),
                    "-D__TEST_DEFINE",
                    $"/Fo:{objDir.Value}/",
                    $"/Fe:{executableFile.Value}",
                    $"/I{pathToIncludes.Value}",
                    $@"/I{win10Include.Value}\ucrt",
                    "/link",
                    $"/LIBPATH:{pathToLibs.Value}",
                    $@"/LIBPATH:{win10Libs.Value}\um\x86",
                    $@"/LIBPATH:{win10Libs.Value}\ucrt\x86"
                ]);
        }
        else
        {
            _output.WriteLine($"Compiling \"{sourceFiles}\" with GCC.");
            await ExecUtil.RunToSuccess(
                _output,
                new("gcc"),
                objDir,
                [
                    ..sourceFiles.Select(x => x.Value),
                    "-o", executableFile.Value,
                    "-D__TEST_DEFINE"
                ]);
        }

        return executableFile;
    }

    private async Task<AbsolutePath> BuildExecutableWithCesium(
        AbsolutePath binDir,
        AbsolutePath objDir,
        IList<AbsolutePath> inputFiles,
        TargetFramework targetFramework,
        TargetArch arch)
    {
        var paths = "[" + string.Join(", ", inputFiles.Select(x => $"\"{x.Value}\"")) + "]";
        _output.WriteLine($"Compiling input files {paths} with Cesium.");

        var executableFilePath = binDir / "out_cs.exe";

        var args = new List<string>([
            "run",
            "--no-build",
            "--configuration", IntegrationTestContext.BuildConfiguration,
            "--project", (SolutionMetadata.SourceRoot / "Cesium.Compiler").Value,
            "--",
            "--nologo",
            ..inputFiles.Select(x => x.Value),
            "--out", executableFilePath.Value,
            "--arch", arch.ToString(),
            "-D__TEST_DEFINE",
            "--framework", targetFramework.ToString()
        ]);

        if (targetFramework == TargetFramework.NetFramework)
        {
            var coreLibPath = WindowsEnvUtil.MsCorLibPath;
            var runtimeLibPath =
                SolutionMetadata.ArtifactsRoot /
                "bin/Cesium.Runtime" /
                $"{IntegrationTestContext.BuildConfiguration.ToLower()}_netstandard2.0" /
                "Cesium.Runtime.dll";
            args.AddRange(new[]
            {
                "--corelib", coreLibPath.Value,
                "--runtime", runtimeLibPath.Value
            });
        }

        await DotNetCliHelper.RunToSuccess(_output, new LocalPath("dotnet"), objDir, args.ToArray());

        return executableFilePath;
    }

    private async Task<AbsolutePath> GenerateJsonObjectFile(
        AbsolutePath objDirPath,
        AbsolutePath sourceFile,
        TargetFramework targetFramework)
    {
        _output.WriteLine($"Generating object file \"{sourceFile.Value}\" with Cesium.");

        var objectFile = objDirPath / Path.ChangeExtension(sourceFile.FileName, ".obj");

        var args = new List<string>
        {
            "run",
            "--no-build",
            "--configuration", IntegrationTestContext.BuildConfiguration,
            "--project", (SolutionMetadata.SourceRoot / "Cesium.Compiler").Value,
            "--",
            "--nologo",
            "-c",
            sourceFile.Value,
            "--out", objectFile.Value,
            "-D__TEST_DEFINE",
            "--framework", targetFramework.ToString()
        };

        await DotNetCliHelper.RunToSuccess(_output, new LocalPath("dotnet"), objDirPath, args.ToArray());

        return objectFile;
    }
}

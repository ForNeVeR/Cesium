using Cesium.Solution.Metadata;
using Cesium.TestFramework;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

public class IntegrationTestRunner : IClassFixture<IntegrationTestContext>, IAsyncLifetime
{
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
        var testCaseDirectory = Path.Combine(SolutionMetadata.SourceRoot, "Cesium.IntegrationTests");
        var cFiles = Directory.EnumerateFileSystemEntries(testCaseDirectory, "*.c", SearchOption.AllDirectories);
        return cFiles
            .Where(IsFileValid)
            .Select(file => Path.GetRelativePath(testCaseDirectory, file))
            .Select(path => new object[] { path });
    }

    private static bool IsFileValid(string file)
    {
        return !file.EndsWith(".ignore.c") && !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && file.EndsWith(".msvc_ignore.c"));
    }

    private enum TargetFramework
    {
        NetFramework,
        Net
    }

    [Theory]
    [MemberData(nameof(TestCaseProvider))]
    public async Task TestNetFramework(string relativeSourcePath)
    {
        if (OperatingSystem.IsWindows())
        {
            await _context.WrapTestBody(() => DoTest(relativeSourcePath, TargetFramework.NetFramework));
        }
    }

    [Theory]
    [MemberData(nameof(TestCaseProvider))]
    public Task TestNet(string relativeSourcePath) => _context.WrapTestBody(() => DoTest(relativeSourcePath, TargetFramework.Net));

    private async Task DoTest(string relativeSourcePath, TargetFramework targetFramework)
    {
        var outRootPath = CreateTempDir();
        try
        {
            _output.WriteLine($"Testing file \"{relativeSourcePath}\" in directory \"{outRootPath}\".");

            var binDirPath = Path.Combine(outRootPath, "bin");
            var objDirPath = Path.Combine(outRootPath, "obj");
            Directory.CreateDirectory(binDirPath);
            Directory.CreateDirectory(objDirPath);

            var sourceFilePath = Path.Combine(
                SolutionMetadata.SourceRoot,
                "Cesium.IntegrationTests",
                relativeSourcePath);

            var nativeExecutable = await BuildExecutableWithNativeCompiler(binDirPath, objDirPath, sourceFilePath);
            var nativeResult = await ExecUtil.Run(_output, nativeExecutable, outRootPath, Array.Empty<string>());
            Assert.Equal(42, nativeResult.ExitCode);

            var managedExecutable = await BuildExecutableWithCesium(
                binDirPath,
                objDirPath,
                sourceFilePath,
                targetFramework);
            var managedResult = await (targetFramework switch
            {
                TargetFramework.Net => DotNetCliHelper.RunDotNetDll(_output, outRootPath, managedExecutable),
                TargetFramework.NetFramework => ExecUtil.Run(_output, managedExecutable, outRootPath,
                    Array.Empty<string>()),
                _ => throw new ArgumentOutOfRangeException(nameof(targetFramework), targetFramework, null)
            });

            Assert.Equal(42, managedResult.ExitCode);

            Assert.Equal(
                nativeResult.StandardOutput.ReplaceLineEndings("\n"),
                managedResult.StandardOutput.ReplaceLineEndings("\n"));
            Assert.Empty(nativeResult.StandardError);
            Assert.Empty(managedResult.StandardError);
        }
        finally
        {
            Directory.Delete(outRootPath, recursive: true);
        }
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

    private async Task<string> BuildExecutableWithNativeCompiler(
        string binDirPath,
        string objDirPath,
        string sourceFilePath)
    {
        var executableFilePath = Path.Combine(binDirPath, "out_native.exe");
        if (OperatingSystem.IsWindows())
        {
            _output.WriteLine($"Compiling \"{sourceFilePath}\" with cl.exe.");

            var vcInstallationFolder = _context.VisualStudioPath;
            Assert.NotNull(vcInstallationFolder);

            var clExePath = Path.Combine(vcInstallationFolder, @"bin\Hostx86\x86\cl.exe");
            var pathToLibs = Path.Combine(vcInstallationFolder, @"lib\x86");
            var pathToIncludes = Path.Combine(vcInstallationFolder, "include");
            var win10SdkPath = WindowsEnvUtil.FindWin10Sdk();
            string win10Libs = WindowsEnvUtil.FindLibsFolder(win10SdkPath);
            string win10Include = WindowsEnvUtil.FindIncludeFolder(win10SdkPath);
            await ExecUtil.RunToSuccess(
                _output,
                clExePath,
                objDirPath,
                new[]
                {
                    "/nologo",
                    sourceFilePath,
                    "-D__TEST_DEFINE",
                    $"/Fo:{objDirPath}/",
                    $"/Fe:{executableFilePath}",
                    $"/I{pathToIncludes}",
                    $@"/I{win10Include}\ucrt",
                    "/link",
                    $"/LIBPATH:{pathToLibs}",
                    $@"/LIBPATH:{win10Libs}\um\x86",
                    $@"/LIBPATH:{win10Libs}\ucrt\x86"
                });
        }
        else
        {
            _output.WriteLine($"Compiling \"{sourceFilePath}\" with GCC.");
            await ExecUtil.RunToSuccess(
                _output,
                "gcc",
                objDirPath,
                new[]
                {
                    sourceFilePath,
                    "-o", executableFilePath,
                    "-D__TEST_DEFINE"
                });
        }

        return executableFilePath;
    }

    private async Task<string> BuildExecutableWithCesium(
        string binDirPath,
        string objDirPath,
        string sourceFilePath,
        TargetFramework targetFramework)
    {
        _output.WriteLine($"Compiling \"{sourceFilePath}\" with Cesium.");

        var executableFilePath = Path.Combine(binDirPath, "out_cs.exe");

        var args = new List<string>
        {
            "run",
            "--no-build",
            "--configuration", IntegrationTestContext.BuildConfiguration,
            "--project", Path.Combine(SolutionMetadata.SourceRoot, "Cesium.Compiler"),
            "--",
            "--nologo",
            sourceFilePath,
            "--out", executableFilePath,
            "-D__TEST_DEFINE",
            "--framework", targetFramework.ToString()
        };

        if (targetFramework == TargetFramework.NetFramework)
        {
            var coreLibPath = WindowsEnvUtil.MsCorLibPath;
            var runtimeLibPath = Path.Combine(
                SolutionMetadata.ArtifactsRoot,
                "bin/Cesium.Runtime",
                $"{IntegrationTestContext.BuildConfiguration.ToLower()}_netstandard2.0",
                "Cesium.Runtime.dll"
            );
            args.AddRange(new[]
            {
                "--corelib", coreLibPath,
                "--runtime", runtimeLibPath
            });
        }

        await DotNetCliHelper.RunToSuccess(_output, "dotnet", objDirPath, args.ToArray());

        return executableFilePath;
    }
}

using Xunit;
using Xunit.Abstractions;

namespace Cesium.IntegrationTests;

public class IntegrationTestRunner : IClassFixture<IntegrationTestContext>
{
    private readonly ITestOutputHelper _output;
    private readonly string _solutionRootPath;
    public IntegrationTestRunner(IntegrationTestContext context, ITestOutputHelper output)
    {
        _output = output;
        context.EnsureInitialized(output);
        _solutionRootPath = context.SolutionRootPath;
    }

    [Theory]
    [InlineData("arithmetics.c")]
    public void TestCompiler(string relativeFilePath)
    {
        var outRootPath = CreateTempDir();
        try
        {
            _output.WriteLine($"Testing file \"{relativeFilePath}\" in directory \"{outRootPath}\".");

            var binDirPath = Path.Combine(outRootPath, "bin");
            var objDirPath = Path.Combine(outRootPath, "obj");
            Directory.CreateDirectory(binDirPath);
            Directory.CreateDirectory(objDirPath);

            var sourceFilePath = Path.Combine(_solutionRootPath, "Cesium.IntegrationTests", relativeFilePath);

            var nativeExecutable = BuildExecutableWithNativeCompiler(binDirPath, objDirPath, sourceFilePath);
            var nativeResult = ExecUtil.Run(_output, nativeExecutable, outRootPath, Array.Empty<string>());
            Assert.Equal(42, nativeResult.ExitCode);

            var managedExecutable = BuildExecutableWithCesium(binDirPath, objDirPath, sourceFilePath);
            var managedResult = ExecUtil.Run(_output, "dotnet", outRootPath, new[] { managedExecutable }); // TODO: Only .NET for now
            Assert.Equal(42, managedResult.ExitCode);

            Assert.Equal(nativeResult.StandardOutput, managedResult.StandardOutput);
            Assert.Empty(nativeResult.StandardError);
            Assert.Empty(managedResult.StandardError);
        }
        finally
        {
            Directory.Delete(outRootPath, recursive: true);
        }
    }

    private string CreateTempDir()
    {
        var path = Path.GetTempFileName();
        File.Delete(path);
        Directory.CreateDirectory(path);
        return path;
    }

    private string BuildExecutableWithNativeCompiler(
        string binDirPath,
        string objDirPath,
        string sourceFilePath)
    {
        var executableFilePath = Path.Combine(binDirPath, "out_native.exe");
        if (OperatingSystem.IsWindows())
        {
            _output.WriteLine($"Compiling \"{sourceFilePath}\" with cl.exe.");

            var vcInstallationFolder = WindowsEnvUtil.FindVCCompilerInstallationFolder(_output);
            var clExePath = Path.Combine(vcInstallationFolder, @"bin\HostX64\x64\cl.exe");
            var pathToLibs = Path.Combine(vcInstallationFolder, @"lib\x64");
            var pathToIncludes = Path.Combine(vcInstallationFolder, @"include");
            var win10SdkPath = WindowsEnvUtil.FindWin10Sdk();
            string win10Libs = WindowsEnvUtil.FindLibsFolder(win10SdkPath);
            string win10Include = WindowsEnvUtil.FindIncludeFolder(win10SdkPath);
            ExecUtil.RunToSuccess(
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
                    $@"/LIBPATH:{win10Libs}\um\x64",
                    $@"/LIBPATH:{win10Libs}\ucrt\x64"
                });
        }
        else
        {
            _output.WriteLine($"Compiling \"{sourceFilePath}\" with GCC.");
            ExecUtil.RunToSuccess(
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

    private string BuildExecutableWithCesium(
        string binDirPath,
        string objDirPath,
        string sourceFilePath,
        string targetFramework = "Net") // TODO: NET Framework support
    {
        _output.WriteLine($"Compiling \"{sourceFilePath}\" with Cesium.");
        // TODO: Workaround for .NET Framework, see in Run-Tests.ps1

        var executableFilePath = Path.Combine(binDirPath, "out_cs.exe");

        ExecUtil.RunToSuccess(
            _output,
            "dotnet",
            objDirPath,
            new[]
            {
                "run",
                "--no-build",
                "--configuration", IntegrationTestContext.BuildConfiguration,
                "--project", Path.Combine(_solutionRootPath, "Cesium.Compiler"),
                "--",
                "--nologo",
                sourceFilePath,
                "--out", executableFilePath,
                "-D__TEST_DEFINE",
                "--framework", targetFramework
            });

        return executableFilePath;
    }
}

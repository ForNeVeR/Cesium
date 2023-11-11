namespace Cesium.Compiler.Tests
{
    public class CommandLineParsingTest
    {
        [Fact]
        public async Task CompileMainArguments()
        {
            var args = new[] { "C:\\doom-clr\\doom.c", "-o", "C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                Assert.Equal(new[] { "C:\\doom-clr\\doom.c" }, args.InputFilePaths);
                Assert.Equal("C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe", args.OutputFilePath);
                Assert.Empty(reporter.Errors);
                NoInformationalMessages(reporter);
                return Task.FromResult(0);
            });
            NoInformationalMessages(reporter);
            Assert.Empty(reporter.Errors);
            Assert.Equal(0, errorCode);
        }

        [Fact]
        public async Task MissingOutputPath()
        {
            var args = new[] { "C:\\doom-clr\\doom.c" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                return Task.FromResult(0);
            });
            NoInformationalMessages(reporter);
            Assert.Equal(new[] { "Required option 'o, out' is missing." }, reporter.Errors);
            Assert.Equal(3, errorCode);
        }

        [Fact]
        public async Task MissingSourceFiles()
        {
            var args = new[] { "-o", "C:\\doom-clr\\doom.exe" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                return Task.FromResult(0);
            });
            NoInformationalMessages(reporter);
            Assert.Equal(new[] { "Input file paths should be defined." }, reporter.Errors);
            Assert.Equal(2, errorCode);
        }

        [Fact]
        public async Task CompileMultipleCompilationFilesMainArguments()
        {
            var args = new[] { "C:\\folder\\file1.c", "C:\\folder\\file2.c", "-o", "C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                Assert.Equal(new[] { "C:\\folder\\file1.c", "C:\\folder\\file2.c" }, args.InputFilePaths);
                Assert.Equal("C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe", args.OutputFilePath);
                return Task.FromResult(0);
            });
            NoInformationalMessages(reporter);
            Assert.Empty(reporter.Errors);
            Assert.Equal(0, errorCode);
        }

        [Fact]
        public async Task PreprocessFile()
        {
            var args = new[] { "C:\\Cesium\\Cesium.Samples\\getopt.c", "-E" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                Assert.Equal(new[] { "C:\\Cesium\\Cesium.Samples\\getopt.c" }, args.InputFilePaths);
                Assert.True(args.ProducePreprocessedFile);
                return Task.FromResult(0);
            });
            Assert.Empty(reporter.InformationMessages);
            Assert.Empty(reporter.Errors);
            Assert.Equal(0, errorCode);
        }

        [Fact]
        public async Task MultipleIncludeFiles()
        {
            var args = new[] { "C:\\Cesium\\Cesium.Samples\\getopt.c", "-o", "C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe", "-I", "C:\\\\Cesium\\\\Cesium.Samples\\\\", "-I", "C:\\Program Files (x86)\\Windows Kits\\10\\Include\\10.0.22621.0\\um\\" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                Assert.Equal(new[] { "C:\\Cesium\\Cesium.Samples\\getopt.c" }, args.InputFilePaths);
                Assert.Equal("C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe", args.OutputFilePath);
                Assert.Equal(new[] { "C:\\\\Cesium\\\\Cesium.Samples\\\\", "C:\\Program Files (x86)\\Windows Kits\\10\\Include\\10.0.22621.0\\um\\" }, args.IncludeDirectories);
                return Task.FromResult(0);
            });
            NoInformationalMessages(reporter);
            Assert.Empty(reporter.Errors);
            Assert.Equal(0, errorCode);
        }

        [Fact]
        public async Task MultipleDefinesFiles()
        {
            var args = new[] { "C:\\Cesium\\Cesium.Samples\\getopt.c", "-o", "C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe", "-D", "TEST_1", "-D", "TEST_2" };
            var reporter = new MockCompilerReporter();

            var errorCode = await CommandLineParser.ParseCommandLineArgs(args, reporter, args =>
            {
                Assert.Equal(new[] { "C:\\Cesium\\Cesium.Samples\\getopt.c" }, args.InputFilePaths);
                Assert.Equal("C:\\\\Cesium\\\\Cesium.IntegrationTests/bin/doom.exe", args.OutputFilePath);
                Assert.Equal(new[] { "TEST_1", "TEST_2" }, args.DefineConstant);
                return Task.FromResult(0);
            });
            NoInformationalMessages(reporter);
            Assert.Empty(reporter.Errors);
            Assert.Equal(0, errorCode);
        }

        private static void NoInformationalMessages(MockCompilerReporter reporter)
        {
            Assert.NotNull(reporter.InformationMessages.Single(_ => _.StartsWith("Cesium v")));
            Assert.Empty(reporter.InformationMessages.Where(_ => !_.StartsWith("Cesium v")));
        }
    }
}

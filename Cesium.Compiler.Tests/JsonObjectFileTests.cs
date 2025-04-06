// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen;
using Cesium.TestFramework;
using Mono.Cecil;
using TruePath;

namespace Cesium.Compiler.Tests;

public class JsonObjectFileTests : VerifyTestBase
{
    [Theory, NoVerify]
    [InlineData("file.json", false)]
    [InlineData("file.obj", true)]
    public void CorrectExtensions(string fileName, bool result) =>
        Assert.Equal(result, JsonObjectFile.IsCorrectExtension(new LocalPath(fileName)));

    private readonly LocalPath[] _inputFiles =
    [
        new("/nonexistent-folder/file1.c"),
        new("file2.c")
    ];

    private readonly CompilationOptions _options = new(
        TargetRuntimeDescriptor.NetStandard20,
        TargetArchitectureSet.Dynamic,
        ModuleKind.Dll,
        new("/corLib.dll"),
        new("/cesiumRuntime.dll"),
        [
            new("ref1.dll"),
            new("/nonexistent-folder/ref2.dll")
        ],
        "My.Namespace",
        "My.Global.Class",
        ["CONSTANT1", "CONSTANT2"],
        [
            new("/nonexistent-folder/include")
        ],
        ProducePreprocessedFile: false,
        ProduceAstFile: true
    );

    [Fact]
    public async Task ObjectFileGetsDumpedCorrectly()
    {
        var outFile = Temporary.CreateTempFile();
        try
        {
            await JsonObjectFile.Write(_inputFiles, _options, outFile);

            var content = await File.ReadAllTextAsync(outFile.Value);
            await Verify(Normalize(content), GetSettings());
        }
        finally
        {
            File.Delete(outFile.Value);
        }

        static string Normalize(string s) => s.Replace(@"\\", "/");
    }

    [Fact, NoVerify]
    public async Task ObjectFileGetsReadCorrectly()
    {
        var objectFile = Temporary.CreateTempFile();
        try
        {
            await JsonObjectFile.Write(_inputFiles, _options, objectFile);
            var content = await JsonObjectFile.Read(objectFile);
            Assert.Equal(_inputFiles, content.InputFilePaths.Select(x => new LocalPath(x)));
            Assert.Equal(_options, content.CompilationOptions);
        }
        finally
        {
            File.Delete(objectFile.Value);
        }
    }
}

using TruePath;

namespace Cesium.Compiler.Tests;

public class JsonObjectFileTests
{
    [Theory]
    [InlineData("file.json", false)]
    [InlineData("file.obj", true)]
    public void CorrectExtensions(string fileName, bool result) =>
        Assert.Equal(result, JsonObjectFile.IsCorrectExtension(new LocalPath(fileName)));

    [Fact]
    public void ObjectFileGetsDumpedCorrectly() => Assert.Fail();

    [Fact]
    public void ObjectFileGetsReadCorrectly() => Assert.Fail();

    [Fact]
    public void CompilationSucceedsForObjectFile() => Assert.Fail("Migrate me to the integration tests");
}

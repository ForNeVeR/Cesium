using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

/// <summary>
/// Tests that proper MSBuild items and properties are populated and set for the Cesium project
/// </summary>
/// <param name="testOutputHelper"></param>
public class EvaluationTests(ITestOutputHelper testOutputHelper) : SdkTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("SimpleCoreLibraryWithHeader")]
    public async Task Evaluation_EnableDefaultCompileItems(string projectName)
    {
        HashSet<string> expectedCompileItems = ["library.c", "library.h"];

        var items = await ListItems(projectName, "Compile");

        Assert.Equal(expectedCompileItems, items.ToHashSet());
    }

    [Theory]
    [InlineData("SimpleExplicitCompileItems")]
    public async Task Evaluation_DisableDefaultCompileItems(string projectName)
    {
        HashSet<string> expectedCompileItems = ["hello.c"];

        var items = await ListItems(projectName, "Compile");

        Assert.Equal(expectedCompileItems, items.ToHashSet());
    }
}

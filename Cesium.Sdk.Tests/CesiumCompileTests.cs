using Xunit.Abstractions;

namespace Cesium.Sdk.Tests;

public class CesiumCompileTests(ITestOutputHelper testOutputHelper) : SdkTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("SimpleProject")]
    public void CesiumCompile_ShouldSucceed(string projectName)
    {
        var result = ExecuteTargets(projectName, "Build");

        Assert.True(result.ExitCode == 0);
        ClearOutput();
    }
}

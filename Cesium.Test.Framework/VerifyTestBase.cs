namespace Cesium.Test.Framework;

[UsesVerify]
public abstract class VerifyTestBase
{
    static VerifyTestBase()
    {
        // To disable Visual Studio popping up on every test execution.
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
    }
}

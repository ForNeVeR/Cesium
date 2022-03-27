namespace Cesium.Test.Framework;

[UsesVerify]
public abstract class VerifyTestBase
{
    static VerifyTestBase()
    {
        // To disable Visual Studio popping up on every test execution.
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
        Environment.SetEnvironmentVariable("Verify_DisableClipboard", "true");
    }

    protected static VerifySettings GetSettings()
    {
        var settings = new VerifySettings();
        settings.UseDirectory("verified");
        return settings;
    }
}

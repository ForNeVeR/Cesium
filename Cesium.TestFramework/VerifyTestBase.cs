// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.TestFramework;

public abstract class VerifyTestBase
{
    static VerifyTestBase()
    {
        // To disable Visual Studio popping up on every test execution.
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
        Environment.SetEnvironmentVariable("Verify_DisableClipboard", "true");

        // To prevent from adding UTF-8 BOM to generated test data:
        VerifierSettings.UseUtf8NoBom();
    }

    protected static VerifySettings GetSettings(params object?[] parameters)
    {
        var settings = new VerifySettings();
        settings.UseDirectory("verified");
        if (parameters.Length > 0)
            settings.UseParameters(parameters);
        return settings;
    }
}

// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.TestFramework.Tests;

public class AssemblyVerificationTest
{
    [Fact] public void UnusedFileShouldBeDetected()
    {
        var exception = Assert.Throws<TestFileVerificationException>(() =>
            TestFileVerification.Verify([typeof(AssemblyVerificationTest)]));
        Assert.Contains("test1.verified.txt", exception.Message);
    }
}

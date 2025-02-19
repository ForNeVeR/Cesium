// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Reflection;
using Cesium.TestFramework;

namespace Cesium.CodeGen.Tests;

public class TestFileVerifierTests
{
    [Fact]
    public void AssemblyHasNoUnusedTestFiles() =>
        TestFileVerification.VerifyAllTestsFromAssembly(Assembly.GetExecutingAssembly());
}

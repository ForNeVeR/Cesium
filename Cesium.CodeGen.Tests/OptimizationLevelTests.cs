// SPDX-FileCopyrightText: 2025-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Cesium.TestFramework;

namespace Cesium.CodeGen.Tests;

public class OptimizationLevelTests : CodeGenTestBase
{
    [NoVerify, Theory] // Only using Assert, no verified.txt file necessary
    [InlineData(OptimizationLevel.NoOp, true)]
    [InlineData(OptimizationLevel.BasicOp, false)]
    public void DebuggableAttributeMatchesOptimizationLevel(OptimizationLevel optimizationLevel, bool expectedDisabled)
    {
        // Throw out assembly bytes since test relies only on assembly definition
        var (assembly, _) = GenerateAssembly(["int main() {}"], optimizationLevel: optimizationLevel);

        // Compare FullName because Cecil's types and .NET types are different objects, but names match
        var debuggableAttribute = assembly.CustomAttributes.Single(attribute => attribute.AttributeType.FullName == typeof(DebuggableAttribute).FullName);

        // Cast attribute as boolean because Value's type cannot be determined at compile time
        Assert.Equal(expectedDisabled, (bool)debuggableAttribute.ConstructorArguments[0].Value);
        Assert.Equal(expectedDisabled, (bool)debuggableAttribute.ConstructorArguments[1].Value);
    }
}

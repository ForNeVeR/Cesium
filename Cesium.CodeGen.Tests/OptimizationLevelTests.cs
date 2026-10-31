// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Cesium.TestFramework;

namespace Cesium.CodeGen.Tests;

public class OptimizationLevelTests : CodeGenTestBase
{
    [NoVerify, Theory]
    [InlineData(OptimizationLevel.NoOp, true)]
    [InlineData(OptimizationLevel.BasicOp, false)]
    public void DebuggableAttributeMatchesOptimizationLevel(OptimizationLevel optimizationLevel, bool expectedDisabled)
    {
        var (assembly, _) = GenerateAssembly(["int main() {}"], optimizationLevel: optimizationLevel);

        // Compare FullName because Cecil's types and .NET types are different objects, but names match
        var debuggableAttribute = assembly.CustomAttributes.Single(attribute => attribute.AttributeType.FullName == typeof(DebuggableAttribute).FullName);

        Assert.Equal(expectedDisabled, (bool)debuggableAttribute.ConstructorArguments[0].Value);
        Assert.Equal(expectedDisabled, (bool)debuggableAttribute.ConstructorArguments[1].Value);
    }
}

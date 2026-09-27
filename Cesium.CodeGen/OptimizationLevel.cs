// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen;

public enum OptimizationLevel
{
    NoOp = 0,
    BasicOp = 1
}

public static class OptimizationLevelExtensions
{
    // Returns the DebuggableAttribute required in AssemblyContext Create() using an extension method since optimizationLevel is an Enum (already existing type)
    public static CustomAttribute GetDebuggableAttribute(this OptimizationLevel optimizationLevel, AssemblyContext context)
    {

        var debuggableAttributeRef = context.Module.ImportReference(new TypeReference("System.Diagnostics", "DebuggableAttribute", context.MscorlibAssembly.MainModule, context.MscorlibAssembly.MainModule));
        var constructorRef = new MethodReference(".ctor", context.Module.TypeSystem.Void, debuggableAttributeRef);
        constructorRef.HasThis = true;

        // Debuggable attribute requires two booleans for specified tracking and optimization
        constructorRef.Parameters.Add(new ParameterDefinition(context.Module.TypeSystem.Boolean));
        constructorRef.Parameters.Add(new ParameterDefinition(context.Module.TypeSystem.Boolean));

        constructorRef = context.Module.ImportReference(constructorRef);

        var disableOptimizations = optimizationLevel switch
        {
            OptimizationLevel.NoOp => true,
            OptimizationLevel.BasicOp => false,
            _ => throw new CompilationException($"Unknown optimization level: {optimizationLevel}")
        };

        return new CustomAttribute(constructorRef)
        {
            ConstructorArguments =
            {
                new CustomAttributeArgument(context.Module.TypeSystem.Boolean, disableOptimizations),
                new CustomAttributeArgument(context.Module.TypeSystem.Boolean, disableOptimizations),
            },
        };
    }
}



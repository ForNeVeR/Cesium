using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Declarations;
using static Cesium.CodeGen.Generators.Statements;

namespace Cesium.CodeGen.Generators;

internal static class Functions
{
    public static void EmitFunction(TranslationUnitContext context, FunctionDefinition definition)
    {
        var method = GenerateMethod(context, definition);
        context.ModuleType.Methods.Add(method);
        if (method.Name == "main")
        {
            var assembly = context.Assembly;
            var currentEntryPoint = assembly.EntryPoint;
            if (currentEntryPoint != null)
                throw new Exception($"Cannot override entrypoint for assembly {assembly} by method {method}.");

            assembly.EntryPoint = method;
        }
    }

    private static MethodDefinition GenerateMethod(TranslationUnitContext context, FunctionDefinition function)
    {
        var functionName = function.Declarator.DirectDeclarator.GetIdentifier();
        var method = new MethodDefinition(
            functionName,
            MethodAttributes.Public | MethodAttributes.Static,
            function.GetReturnType(context.Module.TypeSystem));

        context.Functions.Add(functionName, method);
        var scope = new FunctionScope(context, method);

        if (functionName == "main")
            EmitMainFunction(scope, function);
        else
            EmitFunction(scope, function);

        return method;
    }

    private static void EmitMainFunction(FunctionScope scope, FunctionDefinition function)
    {
        var module = scope.Module;
        var typeSystem = module.TypeSystem;
        var functionName = function.Declarator.DirectDeclarator.GetIdentifier();

        var returnType = function.GetReturnType(typeSystem);
        if (returnType != typeSystem.Int32)
            throw new NotSupportedException(
                $"Invalid return type for the {functionName} function: " +
                $"int expected, got {returnType}.");

        var parameterTypes = function.GetParameterTypes(typeSystem).ToList();
        switch (parameterTypes.Count)
        {
            case 0:
                // It's okay to have no parameters for the main function.
                break;
            case 2:
                switch (parameterTypes[0], parameterTypes[1])
                {
                    case (var @int, PointerType { ElementType: PointerType { ElementType: var @char } })
                        when @int.Equals(typeSystem.Int32) && @char.Equals(typeSystem.Byte):
                        // TODO: Prepare 2-argument main call spot.
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Invalid parameter types for the {functionName} function: " +
                            "int, char*[] expected.");
                }
                break;
            default:
                throw new NotSupportedException(
                    $"Invalid parameter count for the {functionName} function: " +
                    $"2 expected, got {parameterTypes.Count}.");
        }

        if (function.Statement.Block.IsEmpty)
        {
            // TODO: Better definite return analysis.
            var instructions = scope.Method.Body.Instructions;
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else
        {
            EmitFunction(scope, function);
        }
    }

    private static void EmitFunction(FunctionScope scope, FunctionDefinition function)
    {
        foreach (var blockItem in function.Statement.Block)
        {
            switch (blockItem)
            {
                case Declaration d:
                    EmitLocalDeclaration(scope, d);
                    break;
                case Statement s:
                    EmitStatement(scope, s);
                    break;
                default:
                    throw new Exception($"Block item not supported: {blockItem}.");
            }
        }
    }
}

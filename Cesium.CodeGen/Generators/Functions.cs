using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using static Cesium.CodeGen.Generators.Declarations;
using static Cesium.CodeGen.Generators.Statements;

namespace Cesium.CodeGen.Generators;

internal static class Functions
{
    public static void EmitFunction(TranslationUnitContext context, FunctionDefinition definition)
    {
        var (method, isMain) = GenerateMethod(context, definition);
        context.ModuleType.Methods.Add(method);
        if (isMain)
        {
            var assembly = context.Assembly;
            var currentEntryPoint = assembly.EntryPoint;
            if (currentEntryPoint != null)
                throw new Exception($"Cannot override entrypoint for assembly {assembly} by method {method}.");

            assembly.EntryPoint = method;
        }
    }

    private static (MethodDefinition, bool isMain) GenerateMethod(TranslationUnitContext context, FunctionDefinition function)
    {
        var functionName = function.Declarator.DirectDeclarator.GetIdentifier();
        var method = new MethodDefinition(
            functionName,
            MethodAttributes.Public | MethodAttributes.Static,
            function.GetReturnType(context.Module.TypeSystem));

        context.Functions.Add(functionName, method);
        var scope = new FunctionScope(context, method);

        var parameters = function.GetParameters(context.TypeSystem).ToList();
        foreach (var parameter in parameters)
        {
            method.Parameters.Add(parameter);
            if (parameter.Name != null)
                scope.Parameters.Add(parameter.Name, parameter);
        }

        if (functionName == "main")
            return (EmitMainFunction(scope, function), true);

        EmitFunction(scope, function);
        return (method, false);
    }

    private static MethodDefinition EmitMainFunction(FunctionScope scope, FunctionDefinition function)
    {
        var module = scope.Module;
        var typeSystem = module.TypeSystem;
        var functionName = function.Declarator.DirectDeclarator.GetIdentifier();

        var returnType = function.GetReturnType(typeSystem);
        if (returnType != typeSystem.Int32)
            throw new NotSupportedException(
                $"Invalid return type for the {functionName} function: " +
                $"int expected, got {returnType}.");

        MethodDefinition entrypoint;
        var parameterTypes = function.GetParameters(typeSystem).ToList();
        switch (parameterTypes.Count)
        {
            case 0:
                // It's okay to have no parameters for the main function.
                entrypoint = scope.Method;
                break;
            case 2:
                switch (parameterTypes[0].ParameterType, parameterTypes[1].ParameterType)
                {
                    case (var @int, PointerType { ElementType: PointerType { ElementType: var @char } })
                        when @int.Equals(typeSystem.Int32) && @char.Equals(typeSystem.Byte):
                        // TODO: Prepare 2-argument main call spot.
                        scope.Context.ModuleType.Methods.Add(scope.Method);
                        entrypoint = EmitSyntheticEntrypoint(scope.Context, scope.Method);
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

        return entrypoint;
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

    /// <summary>
    /// One of the standard signatures for the main function is <code>int main(int argc, char *argv[])</code>. This
    /// isn't directly supported by the CLI infrastructure, so we'll have to emit a synthetic entrypoint for this case,
    /// which will accept a managed string array and prepare the arguments for the C function.
    /// </summary>
    /// <returns>A synthetic entrypoint method created.</returns>
    private static MethodDefinition EmitSyntheticEntrypoint(
        TranslationUnitContext context,
        MethodReference userEntrypoint)
    {
        var module = context.Module;
        var syntheticEntrypoint = new MethodDefinition(
            "<SyntheticEntrypoint>",
            MethodAttributes.Public | MethodAttributes.Static,
            context.TypeSystem.Int32)
        {
            Parameters =
            {
                new ParameterDefinition("args", ParameterAttributes.None, context.TypeSystem.String.MakeArrayType())
            }
        };

        var bytePtrType = context.TypeSystem.Byte.MakePointerType();
        var bytePtrArrayType = bytePtrType.MakeArrayType();
        var argsToArgv = module.ImportReference(typeof(RuntimeHelpers).GetMethod("ArgsToArgv"));
        var freeArgv = module.ImportReference(typeof(RuntimeHelpers).GetMethod("FreeArgv"));
        var arrayCopyTo = module.ImportReference(typeof(byte*[])
            .GetMethod("CopyTo", new[] { typeof(Array), typeof(int) }));

        var argc = new VariableDefinition(context.TypeSystem.Int32); // 0
        syntheticEntrypoint.Body.Variables.Add(argc);
        var argv = new VariableDefinition(bytePtrArrayType); // 1
        syntheticEntrypoint.Body.Variables.Add(argv);
        // to free the initial array and not a potentially changed one at the end:
        var argvCopy = new VariableDefinition(bytePtrArrayType); // 2
        syntheticEntrypoint.Body.Variables.Add(argvCopy);
        var argvPinned = new VariableDefinition(bytePtrArrayType.MakePinnedType()); // 3
        syntheticEntrypoint.Body.Variables.Add(argvPinned);

        var instructions = syntheticEntrypoint.Body.Instructions;

        // argc = args.Length;
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Ldlen));
        instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        // argv = Cesium.Runtime.RuntimeHelpers.ArgsToArgv(args);
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Call, argsToArgv));
        instructions.Add(Instruction.Create(OpCodes.Stloc_1));

        // try
        Instruction tryStart, tryEnd;
        {
            // argvCopy = new byte*[argc];
            instructions.Add(tryStart = Instruction.Create(OpCodes.Ldloc_0));
            instructions.Add(Instruction.Create(OpCodes.Newarr, bytePtrType));
            instructions.Add(Instruction.Create(OpCodes.Stloc_2));
            // argv.CopyTo(argvCopy, 0);
            instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
            instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            instructions.Add(Instruction.Create(OpCodes.Call, arrayCopyTo));
            // fixed (byte** argvPtr = argvCopy)
            //     return main(argc, argvPtr);
            instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
            Instruction pinStart, pinEnd;
            // pins
            {
                instructions.Add(pinStart = Instruction.Create(OpCodes.Ldloc_2));
                instructions.Add(Instruction.Create(OpCodes.Stloc_3));
                instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
                instructions.Add(Instruction.Create(OpCodes.Ldelema, bytePtrType));
                instructions.Add(Instruction.Create(OpCodes.Call, userEntrypoint));
                instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            // finally: unpin
            {
                Instruction unpinStart, unpinEnd;

                // Cesium.Runtime.RuntimeHelpers.FreeArgv(argv);
                instructions.Add(pinEnd = unpinStart = Instruction.Create(OpCodes.Ldnull));
                instructions.Add(Instruction.Create(OpCodes.Stloc_3));
                instructions.Add(unpinEnd = Instruction.Create(OpCodes.Endfinally));

                var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = pinStart,
                    TryEnd = pinEnd,
                    HandlerStart = unpinStart,
                    HandlerEnd = unpinEnd
                };
                syntheticEntrypoint.Body.ExceptionHandlers.Add(exceptionHandler);
            }
        }
        // finally
        {
            Instruction finallyStart, finallyEnd;

            // Cesium.Runtime.RuntimeHelpers.FreeArgv(argv);
            instructions.Add(tryEnd = finallyStart = Instruction.Create(OpCodes.Ldloc_1));
            instructions.Add(Instruction.Create(OpCodes.Call, freeArgv));
            instructions.Add(finallyEnd = Instruction.Create(OpCodes.Endfinally));

            var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                HandlerStart = finallyStart,
                HandlerEnd = finallyEnd
            };
            syntheticEntrypoint.Body.ExceptionHandlers.Add(exceptionHandler);
        }

        return syntheticEntrypoint;
    }
}

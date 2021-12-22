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

    private static MethodDefinition GenerateMethod(TranslationUnitContext context, FunctionDefinition definition)
    {
        var functionName = definition.Declarator.DirectDeclarator.Name;
        var method = new MethodDefinition(
            functionName,
            MethodAttributes.Public | MethodAttributes.Static,
            GetReturnType(context.Module, definition));

        context.Functions.Add(functionName, method);
        var scope = new FunctionScope(context, method);

        if (functionName == "main")
            EmitMainFunction(scope, definition);
        else
            EmitFunction(scope, definition);

        return method;
    }

    private static TypeReference GetReturnType(ModuleDefinition module, FunctionDefinition definition)
    {
        var typeSpecifier = definition.Specifiers.OfType<TypeSpecifier>().Single();
        return typeSpecifier.GetTypeReference(module);
    }

    private static void EmitMainFunction(FunctionScope scope, FunctionDefinition function)
    {
        // TODO: Alternate signature support.
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

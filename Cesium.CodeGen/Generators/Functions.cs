using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Declarations;
using static Cesium.CodeGen.Generators.Statements;

namespace Cesium.CodeGen.Generators;

internal static class Functions
{
    public static MethodDefinition GenerateMethod(ModuleDefinition module, FunctionDefinition definition)
    {
        var method = new MethodDefinition(
            definition.Declarator.DirectDeclarator.Name,
            MethodAttributes.Public | MethodAttributes.Static,
            GetReturnType(module, definition));
        var scope = new FunctionScope(module, method);

        if (definition.Declarator.DirectDeclarator.Name == "main")
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
                    EmitDeclaration(scope, d);
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

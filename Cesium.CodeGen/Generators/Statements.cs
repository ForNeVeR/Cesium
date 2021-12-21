using Cesium.Ast;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Expressions;

namespace Cesium.CodeGen.Generators;

internal static class Statements
{
    public static void EmitStatement(MethodDefinition method, Statement statement)
    {
        switch (statement)
        {
            case ReturnStatement r:
                EmitReturnStatement(method, r);
                break;
            default:
                throw new Exception($"Statement not supported: {statement}.");
        }
    }

    private static void EmitReturnStatement(MethodDefinition method, ReturnStatement statement)
    {
        EmitExpression(method, statement.Expression);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}

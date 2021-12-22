using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Expressions;

namespace Cesium.CodeGen.Generators;

internal static class Statements
{
    public static void EmitStatement(FunctionScope scope, Statement statement)
    {
        switch (statement)
        {
            case ExpressionStatement e:
                EmitExpressionStatement(scope, e);
                break;
            case ReturnStatement r:
                EmitReturnStatement(scope, r);
                break;
            default:
                throw new Exception($"Statement not supported: {statement}.");
        }
    }

    private static void EmitExpressionStatement(FunctionScope scope, ExpressionStatement statement)
    {
        if (statement.Expression != null)
            EmitExpression(scope, statement.Expression);
    }

    private static void EmitReturnStatement(FunctionScope scope, ReturnStatement statement)
    {
        EmitExpression(scope, statement.Expression);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}

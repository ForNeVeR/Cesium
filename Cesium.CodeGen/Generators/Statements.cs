using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Expressions;

namespace Cesium.CodeGen.Generators;

internal static class Statements // TODO[F]: Delete this class
{
    public static void EmitStatement(FunctionScope scope, Statement statement)
    {
        switch (statement)
        {
            case ExpressionStatement e:
                EmitExpressionStatement(scope, e);
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
}

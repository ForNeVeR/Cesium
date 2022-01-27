using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Ast.Expression e) => e switch
    {
        Ast.AssignmentExpression o => new AssignmentExpression(o),
        Ast.BinaryOperatorExpression o => new BinaryOperatorExpression(o),
        // _ => throw new NotImplementedException($"Expression not supported, yet: {e}."),
        _ => new AstExpression(e)
    };
}

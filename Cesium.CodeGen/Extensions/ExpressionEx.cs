using Cesium.CodeGen.Ir.Expressions;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Ast.Expression ex) => ex switch
    {
        Ast.AssignmentExpression e => new AssignmentExpression(e),
        Ast.BinaryOperatorExpression e => new BinaryOperatorExpression(e),
        Ast.ConstantExpression { Constant.Kind: CTokenType.Identifier } e => new IdentifierConstantExpression(e),
        Ast.ConstantExpression e => new ConstantExpression(e),
        // _ => throw new NotImplementedException($"Expression not supported, yet: {e}."),
        _ => new AstExpression(ex)
    };
}

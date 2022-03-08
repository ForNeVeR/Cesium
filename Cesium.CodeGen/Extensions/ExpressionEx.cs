using Cesium.CodeGen.Ir.Expressions;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Ast.Expression ex) => ex switch
    {
        Ast.ConstantExpression { Constant.Kind: CTokenType.Identifier } e => new IdentifierConstantExpression(e),
        Ast.ConstantExpression e => new ConstantExpression(e),

        Ast.FunctionCallExpression e => new FunctionCallExpression(e),

        // Unary operators:
        Ast.PrefixIncrementExpression e => new PrefixIncrementExpression(e),
        Ast.NegationExpression e => new NegationExpression(e),
        Ast.BitwiseNotExpression e => new BitwiseNotExpression(e),

        // Binary operators:
        Ast.AssignmentExpression e => new AssignmentExpression(e),
        Ast.BinaryOperatorExpression e => new BinaryOperatorExpression(e),

        _ => throw new NotImplementedException($"Expression not supported, yet: {ex}."),
    };
}

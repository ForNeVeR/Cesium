using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Ast.Expression ex) => ex switch
    {
        Ast.IdentifierExpression e => new IdentifierExpression(e),
        Ast.ConstantExpression { Constant.Kind: CTokenType.Identifier } e => new IdentifierExpression(e),
        Ast.ConstantExpression e => new ConstantExpression(e),

        Ast.FunctionCallExpression e => new FunctionCallExpression(e),

        // Unary operators:
        Ast.PrefixIncrementExpression e => new PrefixIncrementExpression(e),
        Ast.UnaryOperatorExpression e => new UnaryOperatorExpression(e),

        // Binary operators:
        Ast.AssignmentExpression e => new AssignmentExpression(e),
        Ast.LogicalBinaryOperatorExpression e => new LogicalBinaryOperatorExpression(e),
        Ast.ArithmeticBinaryOperatorExpression e => new ArithmeticBinaryOperatorExpression(e),
        Ast.BitwiseBinaryOperatorExpression e => new BitwiseBinaryOperatorExpression(e),
        Ast.ComparisonBinaryOperatorExpression e => new ComparisonBinaryOperatorExpression(e),

        Ast.SubscriptingExpression e => new SubscriptingExpression(e),
        Ast.MemberAccessExpression e => new MemberAccessExpression(e),
        Ast.PointerMemberAccessExpression e => new PointerMemberAccessExpression(e),

        _ => throw new NotImplementedException($"Expression not supported, yet: {ex}."),
    };
}

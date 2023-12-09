using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Ast.Expression ex) => ex switch
    {
        Ast.IdentifierExpression e => new IdentifierExpression(e),
        Ast.StringLiteralListExpression e => new StringLiteralListExpression(e),
        Ast.ConstantLiteralExpression { Constant.Kind: CTokenType.Identifier } e => new IdentifierExpression(e),
        Ast.ConstantLiteralExpression e => new ConstantLiteralExpression(e),
        Ast.ParenExpression e => ToIntermediate(e.Contents),

        Ast.TypeCastOrNamedFunctionCallExpression e => new TypeCastOrNamedFunctionCallExpression(e),
        Ast.FunctionCallExpression e => new FunctionCallExpression(e),

        // Unary operators:
        Ast.PrefixIncrementDecrementExpression e => new PrefixIncrementDecrementExpression(e),
        Ast.IndirectionExpression e => new IndirectionExpression(e),
        Ast.UnaryOperatorExpression e => new UnaryOperatorExpression(e),
        Ast.CastExpression e => new TypeCastExpression(e),
        Ast.UnaryExpressionSizeOfOperatorExpression e => new ExpressionSizeOfOperatorExpression(e.TargetExpession.ToIntermediate()),
        Ast.TypeNameSizeOfOperatorExpression e => new TypeNameSizeOfOperatorExpression(e),

        // Binary operators:
        Ast.AssignmentExpression e => new AssignmentExpression(e),
        Ast.LogicalBinaryOperatorExpression e => new BinaryOperatorExpression(e),
        Ast.ArithmeticBinaryOperatorExpression e => new BinaryOperatorExpression(e),
        Ast.BitwiseBinaryOperatorExpression e => new BinaryOperatorExpression(e),
        Ast.ComparisonBinaryOperatorExpression e => new BinaryOperatorExpression(e),

        Ast.ConditionalExpression e => new ConditionalExpression(e),

        Ast.SubscriptingExpression e => new SubscriptingExpression(e),
        Ast.MemberAccessExpression e => new MemberAccessExpression(e),
        Ast.PointerMemberAccessExpression e => new PointerMemberAccessExpression(e),
        Ast.PostfixIncrementDecrementExpression e => new PostfixIncrementDecrementExpression(e),

        Ast.CommaExpression e => new CommaExpression(e),

        _ => throw new WipException(208, $"Expression not supported, yet: {ex}."),
    };
}

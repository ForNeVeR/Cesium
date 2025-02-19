// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Ast.Expression ex, IDeclarationScope scope) => ex switch
    {
        Ast.IdentifierExpression e => new IdentifierExpression(e),
        Ast.StringLiteralListExpression e => new StringLiteralListExpression(e),
        Ast.ConstantLiteralExpression { Constant.Kind: CTokenType.Identifier } e => new IdentifierExpression(e),
        Ast.ConstantLiteralExpression e => new ConstantLiteralExpression(e),
        Ast.ParenExpression e => ToIntermediate(e.Contents, scope),

        Ast.TypeCastOrNamedFunctionCallExpression e => new TypeCastOrNamedFunctionCallExpression(e, scope),
        Ast.FunctionCallExpression e => new FunctionCallExpression(e, scope),

        // Unary operators:
        Ast.PrefixIncrementDecrementExpression e => new PrefixIncrementDecrementExpression(e, scope),
        Ast.IndirectionExpression e => new IndirectionExpression(e, scope),
        Ast.UnaryOperatorExpression e => new UnaryOperatorExpression(e, scope),
        Ast.CastExpression e => new TypeCastExpression(e, scope),
        Ast.UnaryExpressionSizeOfOperatorExpression e => new ExpressionSizeOfOperatorExpression(e.TargetExpession.ToIntermediate(scope)),
        Ast.TypeNameSizeOfOperatorExpression e => new TypeNameSizeOfOperatorExpression(e, scope),

        // Binary operators:
        Ast.AssignmentExpression e => new AssignmentExpression(e, true, scope),
        Ast.LogicalBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),
        Ast.ArithmeticBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),
        Ast.BitwiseBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),
        Ast.ComparisonBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),

        Ast.ConditionalExpression e => new ConditionalExpression(e, scope),

        Ast.SubscriptingExpression e => new SubscriptingExpression(e, scope),
        Ast.MemberAccessExpression e => new MemberAccessExpression(e, scope),
        Ast.PointerMemberAccessExpression e => new PointerMemberAccessExpression(e, scope),
        Ast.PostfixIncrementDecrementExpression e => new PostfixIncrementDecrementExpression(e, scope),
        Ast.CompoundLiteralExpression e => new CompoundObjectInitializationExpression(e, scope),

        Ast.CommaExpression e => new CommaExpression(e, scope),

        _ => throw new WipException(208, $"Expression not supported, yet: {ex}."),
    };
}

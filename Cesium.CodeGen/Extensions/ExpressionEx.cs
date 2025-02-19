// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using AssignmentExpression = Cesium.Ast.AssignmentExpression;
using BinaryOperatorExpression = Cesium.CodeGen.Ir.Expressions.BinaryOperators.BinaryOperatorExpression;
using CommaExpression = Cesium.Ast.CommaExpression;
using ConditionalExpression = Cesium.Ast.ConditionalExpression;
using ConstantLiteralExpression = Cesium.Ast.ConstantLiteralExpression;
using FunctionCallExpression = Cesium.Ast.FunctionCallExpression;
using IdentifierExpression = Cesium.Ast.IdentifierExpression;
using IndirectionExpression = Cesium.Ast.IndirectionExpression;
using MemberAccessExpression = Cesium.Ast.MemberAccessExpression;
using PointerMemberAccessExpression = Cesium.Ast.PointerMemberAccessExpression;
using PostfixIncrementDecrementExpression = Cesium.Ast.PostfixIncrementDecrementExpression;
using PrefixIncrementDecrementExpression = Cesium.Ast.PrefixIncrementDecrementExpression;
using StringLiteralListExpression = Cesium.Ast.StringLiteralListExpression;
using SubscriptingExpression = Cesium.Ast.SubscriptingExpression;
using TypeCastOrNamedFunctionCallExpression = Cesium.Ast.TypeCastOrNamedFunctionCallExpression;
using TypeNameSizeOfOperatorExpression = Cesium.Ast.TypeNameSizeOfOperatorExpression;
using UnaryOperatorExpression = Cesium.Ast.UnaryOperatorExpression;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Expression ex, IDeclarationScope scope) => ex switch
    {
        IdentifierExpression e => new Ir.Expressions.IdentifierExpression(e),
        StringLiteralListExpression e => new Ir.Expressions.StringLiteralListExpression(e),
        ConstantLiteralExpression { Constant.Kind: CTokenType.Identifier } e => new Ir.Expressions.IdentifierExpression(e),
        ConstantLiteralExpression e => new Ir.Expressions.ConstantLiteralExpression(e),
        ParenExpression e => ToIntermediate(e.Contents, scope),

        TypeCastOrNamedFunctionCallExpression e => new Ir.Expressions.TypeCastOrNamedFunctionCallExpression(e, scope),
        FunctionCallExpression e => new Ir.Expressions.FunctionCallExpression(e, scope),

        // Unary operators:
        PrefixIncrementDecrementExpression e => new Ir.Expressions.PrefixIncrementDecrementExpression(e, scope),
        IndirectionExpression e => new Ir.Expressions.IndirectionExpression(e, scope),
        UnaryOperatorExpression e => new Ir.Expressions.UnaryOperatorExpression(e, scope),
        CastExpression e => new TypeCastExpression(e, scope),
        UnaryExpressionSizeOfOperatorExpression e => new ExpressionSizeOfOperatorExpression(e.TargetExpession.ToIntermediate(scope)),
        TypeNameSizeOfOperatorExpression e => new Ir.Expressions.TypeNameSizeOfOperatorExpression(e, scope),

        // Binary operators:
        AssignmentExpression e => new Ir.Expressions.AssignmentExpression(e, true, scope),
        LogicalBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),
        ArithmeticBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),
        BitwiseBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),
        ComparisonBinaryOperatorExpression e => new BinaryOperatorExpression(e, scope),

        ConditionalExpression e => new Ir.Expressions.ConditionalExpression(e, scope),

        SubscriptingExpression e => new Ir.Expressions.SubscriptingExpression(e, scope),
        MemberAccessExpression e => new Ir.Expressions.MemberAccessExpression(e, scope),
        PointerMemberAccessExpression e => new Ir.Expressions.PointerMemberAccessExpression(e, scope),
        PostfixIncrementDecrementExpression e => new Ir.Expressions.PostfixIncrementDecrementExpression(e, scope),
        CompoundLiteralExpression e => new CompoundObjectInitializationExpression(e, scope),

        CommaExpression e => new Ir.Expressions.CommaExpression(e, scope),

        _ => throw new WipException(208, $"Expression not supported, yet: {ex}."),
    };
}

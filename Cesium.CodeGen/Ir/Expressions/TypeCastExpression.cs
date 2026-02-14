// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using BinaryOperatorExpression = Cesium.CodeGen.Ir.Expressions.BinaryOperators.BinaryOperatorExpression;
using C = Cesium.CodeGen.Ir.Types.CTypeSystem;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class TypeCastExpression : IExpression
{
    public IType TargetType { get; }
    public IExpression Expression { get; }

    public TypeCastExpression(IType targetType, IExpression expression)
    {
        TargetType = targetType;
        Expression = expression;
    }

    public TypeCastExpression(CastExpression castExpression, IDeclarationScope scope)
    {
        var ls = castExpression.TypeName.AbstractDeclarator is null
            ? LocalDeclarationInfo.Of(castExpression.TypeName.SpecifierQualifierList, (Declarator?)null, initializer: null, scope)
            : LocalDeclarationInfo.Of(castExpression.TypeName.SpecifierQualifierList, castExpression.TypeName.AbstractDeclarator, scope);
        TargetType = ls.Type;
        Expression = ExpressionEx.ToIntermediate(castExpression.Target, scope);
    }

    public void EmitTo(IEmitScope scope)
    {
        if (TargetType is InteropType iType)
        {
            iType.EmitConversion(scope, Expression);
            return;
        }

        Expression.EmitTo(scope);

        if (TargetType.Equals(C.Bool))
        {
            return;
        }

        if (TargetType.Equals(C.SignedChar))
            Add(OpCodes.Conv_I1);
        else if (TargetType.Equals(C.Short))
            Add(OpCodes.Conv_I2);
        else if (TargetType.Equals(C.Int))
            Add(OpCodes.Conv_I4);
        else if (TargetType.Equals(C.Long) || TargetType.Equals(C.LongLong))
            Add(OpCodes.Conv_I8);
        else if (TargetType.Equals(C.Char))
            Add(OpCodes.Conv_U1);
        else if (TargetType.Equals(C.UnsignedChar))
            Add(OpCodes.Conv_U1);
        else if (TargetType.Equals(C.UnsignedShort))
            Add(OpCodes.Conv_U2);
        else if (TargetType.Equals(C.UnsignedInt) || TargetType.Equals(C.Unsigned))
            Add(OpCodes.Conv_U4);
        else if (TargetType.Equals(C.UnsignedLong) || TargetType.Equals(C.UnsignedLongLong))
            Add(OpCodes.Conv_U8);
        else if (TargetType.Equals(C.Float))
            Add(OpCodes.Conv_R4);
        else if (TargetType.Equals(C.Double))
            Add(OpCodes.Conv_R8);
        else if (TargetType is PointerType || TargetType.Equals(C.NativeInt) || TargetType.Equals(C.NativeUInt))
            Add(OpCodes.Conv_I);
        else if (TargetType is EnumType)
            Add(OpCodes.Conv_I4);
        else
            throw new AssertException($"Type {TargetType} is not supported.");

        void Add(OpCode op) => scope.Method.Body.Instructions.Add(Instruction.Create(op));
    }

    public IType GetExpressionType(IDeclarationScope scope) => TargetType;

    public IExpression Lower(IDeclarationScope scope)
    {
        if (TargetType is NamedType namedType)
        {
            var resolvedTypeCandidate = scope.TryGetType(namedType.TypeName);
            if (resolvedTypeCandidate is null)
            {
                if (Expression is UnaryOperatorExpression { Operator: UnaryOperator.Promotion } unaryExpression)
                {
                    return new BinaryOperatorExpression(new IdentifierExpression(namedType.TypeName), BinaryOperator.Add, unaryExpression.Target).Lower(scope);
                }

                if (Expression is CommaExpression commaExpression)
                {
                    List<IExpression> expressions = new();
                    var newCommaExpression = commaExpression;
                    do
                    {
                        commaExpression = newCommaExpression;
                        expressions.Add(commaExpression.Left);
                        newCommaExpression = commaExpression.Right as CommaExpression;
                    }
                    while (newCommaExpression is not null);
                    expressions.Add(commaExpression.Right);
                    return new Expressions.FunctionCallExpression(new IdentifierExpression(namedType.TypeName), null, expressions).Lower(scope);
                }
            }
        }

        if (TargetType is InPlaceArrayType inPlaceArrayType && inPlaceArrayType.Base is NamedType namedType1)
        {
            var resolvedTypeCandidate = scope.TryGetType(namedType1.TypeName);
            if (resolvedTypeCandidate is null)
            {
                if (Expression is UnaryOperatorExpression { Operator: UnaryOperator.Promotion } unaryExpression)
                {
                    return new BinaryOperatorExpression(
                        new SubscriptingExpression(
                            new IdentifierExpression(namedType1.TypeName),
                            new ConstantLiteralExpression(new IntegerConstant(inPlaceArrayType.Size)),
                            false),
                        BinaryOperator.Add,
                        unaryExpression.Target).Lower(scope);
                }
                if (Expression is IndirectionExpression indirectionExpression)
                {
                    return new SubscriptingExpression(
                        new IdentifierExpression(namedType1.TypeName),
                        indirectionExpression.Target,
                        true).Lower(scope);
                }
            }
        }

        var resolvedType = scope.ResolveType(TargetType);

        return resolvedType is PrimitiveType { Kind: PrimitiveTypeKind.Void }
            ? new ConsumeExpression(Expression.Lower(scope)).Lower(scope)
            : new TypeCastExpression(resolvedType, Expression.Lower(scope));
    }
}

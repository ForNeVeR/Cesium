// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class SubscriptingExpression : IValueExpression
{
    internal IExpression Expression { get; }

    internal IExpression Index { get; }
    public bool AddressOnly { get; }

    public SubscriptingExpression(Ast.SubscriptingExpression subscriptingExpression, IDeclarationScope scope)
    {
        var (expression, index) = subscriptingExpression;
        Expression = expression.ToIntermediate(scope);
        Index = index.ToIntermediate(scope);
    }

    public SubscriptingExpression(IExpression expression, IExpression index, bool addressOnly)
    {
        Expression = expression;
        Index = index;
        AddressOnly = addressOnly;
    }

    private static bool CheckIfTypeIsSubscriptable(IType type)
    {
        return type is InPlaceArrayType or PointerType;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var expression = Expression.Lower(scope);
        var index = Index.Lower(scope);
        var expressionType = expression.GetExpressionType(scope);
        var indexType = index.GetExpressionType(scope);

        var isBaseSubscriptable = CheckIfTypeIsSubscriptable(expressionType);
        var isIndexSubscriptable = CheckIfTypeIsSubscriptable(indexType);
        if (!isBaseSubscriptable && isIndexSubscriptable)
        {
            (expression, index) = (index, expression);
            (expressionType, indexType) = (indexType, expressionType);
        }
        else if (!isBaseSubscriptable && !isIndexSubscriptable)
        {
            throw new AssertException($"Cannot index over type {expressionType} or {indexType}");
        }

        IExpression fullExpression;
        switch (expressionType)
        {
            case InPlaceArrayType:
            {
                var arrayExpression = (IValueExpression)expression;
                var arrayValue = arrayExpression.Resolve(scope);
                fullExpression = new GetValueExpression(
                    AddressOnly ? new LValueArrayElementAddress(arrayValue, index)  : new LValueArrayElement(arrayValue, index));
                break;
            }
            case PointerType:
                fullExpression = new IndirectionExpression(
                    new BinaryOperatorExpression(
                        expression,
                        BinaryOperator.Add,
                        index
                    )
                );
                break;
            default:
                throw new CompilationException($"Expression is not subscriptable: {expression}.");
        }

        var lowered = fullExpression.Lower(scope);
        return lowered;
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        if (Expression is IValueExpression valueExpression)
        {
            var newValueExpression = valueExpression.Resolve(scope);
            return AddressOnly
                ? new LValueArrayElementAddress(newValueExpression, Index)
                : new LValueArrayElement(newValueExpression, Index);
        }

        throw new CompilationException($"{Expression} is not a value expression");
    }

    private static int GetElementsSize(InPlaceArrayType inPlaceArray)
    {
        if (inPlaceArray.Base is InPlaceArrayType nestedArray)
        {
            return GetElementsSize(nestedArray) * inPlaceArray.Size;
        }

        return inPlaceArray.Size;
    }
}

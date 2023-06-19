using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SubscriptingExpression : IExpression, IValueExpression
{
    private readonly IExpression _expression;
    private readonly IExpression _index;

    public SubscriptingExpression(Ast.SubscriptingExpression subscriptingExpression)
    {
        var (expression, index) = subscriptingExpression;
        _expression = expression.ToIntermediate();
        _index = index.ToIntermediate();
    }

    private SubscriptingExpression(IExpression expression, IExpression index)
    {
        _expression = expression;
        _index = index;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var expression = LowerExpression(_expression, scope);
        var index = LowerExpression(_index, scope);
        var expressionType = expression.GetExpressionType(scope);
        var indexType = index.GetExpressionType(scope);

        switch ((CheckIfTypeIsSubscriptable(expressionType), CheckIfTypeIsSubscriptable(indexType)))
        {
            case (false, true):
                (expression, index) = (index, expression);
                (expressionType, indexType) = (indexType, expressionType);
                break;
            case (false, false):
                throw new AssertException($"Cannot index over type {expressionType} or {indexType}");
        }

        var elementSize = GetElementSize(expressionType);
        var offset = elementSize != 1
            ? new BinaryOperatorExpression(index, BinaryOperator.Multiply, new ConstantLiteralExpression(new IntegerConstant(elementSize)))
            : index;
        var value = (IAddressableValue)((IValueExpression)expression).Resolve(scope);
        var indirection = new IndirectionExpression(
            new BinaryOperatorExpression(
                value.GetValueType() is InPlaceArrayType ? new GetAddressValueExpression(value) : new GetValueExpression(value),
                BinaryOperator.Add,
                offset.Lower(scope)
            ));
        var lowered = indirection.Lower(scope);
        return lowered;
    }

    private static bool CheckIfTypeIsSubscriptable(IType type)
    {
        return type is InPlaceArrayType or PointerType;
    }

    private static int GetElementSize(IType type)
    {
        if (type is InPlaceArrayType inPlaceArrayType)
        {
            return inPlaceArrayType.Base is InPlaceArrayType nestedArray ? GetElementsSize(nestedArray) : 1;
        }
        else if (type is PointerType pointerType)
        {
            return 1;
        }
        else
        {
            throw new AssertException($"Cannot index over type {type}");
        }
    }

    private static IExpression LowerExpression(IExpression expression, IDeclarationScope scope)
    {
        if (expression is SubscriptingExpression subscriptingExpression)
        {
            var newExpression = LowerExpression(subscriptingExpression._expression, scope);
            return new SubscriptingExpression(newExpression, subscriptingExpression._index.Lower(scope));
        }

        if (expression is MemberAccessExpression)
        {
            return expression.Lower(scope);
        }

        return expression;
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        if (_expression is IdentifierExpression identifier)
        {
            return new LValueArrayElement(identifier.Resolve(scope), _index);
        }

        if (_expression is SubscriptingExpression subscriptingExpression)
        {
            var a = subscriptingExpression.Resolve(scope);
            return new LValueArrayElement(a, _index);
        }

        throw new WipException(230, "Subscription supported only for IdentifierConstantExpression");
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

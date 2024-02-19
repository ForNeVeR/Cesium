using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class SubscriptingExpression : IValueExpression
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

    private static bool CheckIfTypeIsSubscriptable(IType type)
    {
        return type is InPlaceArrayType or PointerType;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var expression = _expression.Lower(scope);
        var index = _index.Lower(scope);
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
                fullExpression = new GetValueExpression(new LValueArrayElement(arrayValue, index));
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
        if (_expression is IValueExpression valueExpression)
        {
            return new LValueArrayElement(valueExpression.Resolve(scope), _index);
        }

        throw new CompilationException($"{_expression} is not a value expression");
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

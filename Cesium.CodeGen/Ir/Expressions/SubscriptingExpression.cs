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
        var expressionType = (InPlaceArrayType)_expression.GetExpressionType(scope);
        var value = (IAddressableValue)((IValueExpression)_expression).Resolve(scope);
        var offset = expressionType.Base is InPlaceArrayType nestedArray
            ? new ArithmeticBinaryOperatorExpression(_index, BinaryOperator.Multiply, new ConstantLiteralExpression(new IntegerConstant(GetElementsSize(nestedArray))))
            : _index;
        var indirection = new IndirectionExpression(
            new ArithmeticBinaryOperatorExpression(
                new GetAddressValueExpression(value),
                BinaryOperator.Add,
                offset
            ));
        var lowered = indirection.Lower(scope);
        return lowered;
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        if (_expression is not IdentifierExpression identifier)
            throw new WipException(230, "Subscription supported only for IdentifierConstantExpression");

        return new LValueArrayElement(identifier.Resolve(scope), _index);
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

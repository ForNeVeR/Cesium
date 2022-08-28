using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class TypeCastExpression : IExpression
{
    private IType _targetType;
    private IExpression _expression;

    public TypeCastExpression(IType targetType, IExpression expression)
    {
        _targetType = targetType;
        _expression = expression;
    }

    public void EmitTo(IDeclarationScope scope)
    {
        _expression.EmitTo(scope);
        BinaryOperatorExpression.EmitConversion(scope, _expression.GetExpressionType(scope), _targetType);
    }

    public IType GetExpressionType(IDeclarationScope scope) => _targetType;

    public IExpression Lower() => this;
}

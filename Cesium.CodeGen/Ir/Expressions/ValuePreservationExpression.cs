using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

/// <summary>
/// Provides access to already loaded value (before expression)
/// </summary>
internal class ValuePreservationExpression : IExpression
{
    private IValue _value;
    private IExpression _expression;

    public ValuePreservationExpression(IValue value, IExpression expression)
    {
        _value = value;
        _expression = expression;
    }

    public IExpression Lower(IDeclarationScope scope) => new ValuePreservationExpression(_value, _expression.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        if (_expression is SetValueExpression sv)
            sv.NoReturn().EmitTo(scope);
        else _expression.EmitTo(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope) => _value.GetValueType();
}

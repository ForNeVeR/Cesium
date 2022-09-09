using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SetValueExpression : IExpression
{
    private readonly ILValue _value;
    private readonly IExpression _expression;

    public SetValueExpression(ILValue value, IExpression expression)
    {
        _value = value;
        _expression = expression;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope) => _value.EmitSetValue(scope, _expression);

    public IType GetExpressionType(IDeclarationScope scope) => _value.GetValueType();
}

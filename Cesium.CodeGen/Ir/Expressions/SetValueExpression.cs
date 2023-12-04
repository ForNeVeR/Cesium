using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class SetValueExpression : IExpression
{
    private readonly ILValue _value;
    private readonly IExpression _expression;
    private readonly bool _doReturn;

    public SetValueExpression(ILValue value, IExpression expression, bool doReturn = true)
    {
        _value = value;
        _expression = expression;
        _doReturn = doReturn;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        _value.EmitSetValue(scope, _expression);

        if (_doReturn)
            _value.EmitGetValue(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope) => _doReturn
        ? _value.GetValueType()
        : new PrimitiveType(PrimitiveTypeKind.Void);

    public IExpression NoReturn() => new SetValueExpression(_value, _expression, false);
}

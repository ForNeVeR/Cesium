using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class GetValueExpression : IValueExpression
{
    private readonly IValue _value;

    public GetValueExpression(IValue value)
    {
        _value = value;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope) => _value.EmitGetValue(scope);

    public IType GetExpressionType(IDeclarationScope scope) => _value.GetValueType();

    public IValue Resolve(IDeclarationScope scope) => _value;
}

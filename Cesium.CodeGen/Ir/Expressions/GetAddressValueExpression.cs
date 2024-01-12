using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class GetAddressValueExpression : IExpression
{
    private readonly IAddressableValue _value;
    public GetAddressValueExpression(IAddressableValue value)
    {
        _value = value;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        _value.EmitGetAddress(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        IType valueType = _value.GetValueType();
        return GetBasePointer(valueType);
    }

    private static IType GetBasePointer(IType valueType)
    {
        return valueType.MakePointerType();
    }
}

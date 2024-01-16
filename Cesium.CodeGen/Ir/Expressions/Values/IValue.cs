using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal interface IValue
{
    void EmitGetValue(IEmitScope scope);
    IType GetValueType();
}

/// <remarks>
/// This is different from <see cref="ILValue"/> because functions are not lvalues but still can be addressed.
/// </remarks>
internal interface IAddressableValue : IValue
{
    void EmitGetAddress(IEmitScope scope);
}

internal interface ILValue : IAddressableValue
{
    void EmitSetValue(IEmitScope scope, IExpression value);
}

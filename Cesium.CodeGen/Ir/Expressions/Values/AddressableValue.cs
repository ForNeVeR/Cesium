using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions.Values;

/// <remarks>For in-place arrays, GetAddress and GetValue are the same.</remarks>
internal abstract class AddressableValue : IAddressableValue
{
    public abstract IType GetValueType();

    public void EmitGetAddress(IEmitScope scope)
    {
        if (GetValueType() is InPlaceArrayType)
        {
            EmitGetValue(scope);
            return;
        }

        EmitGetAddressUnchecked(scope);
    }

    /// <remarks>This method doesn't have to check for in-place arrays.</remarks>
    protected abstract void EmitGetAddressUnchecked(IEmitScope scope);

    public abstract void EmitGetValue(IEmitScope scope);
}

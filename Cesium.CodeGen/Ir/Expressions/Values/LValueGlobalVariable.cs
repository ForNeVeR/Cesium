using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueGlobalVariable : LValueField
{
    private readonly IType _type;
    private readonly string _name;
    private FieldReference? _field;

    public LValueGlobalVariable(IType type, string name)
    {
        _type = type;
        _name = name;
    }

    public override IType GetValueType() => _type;

    protected override void EmitGetFieldOwner(IEmitScope scope)
    {
        // No field owner since the field is static.
    }

    protected override FieldReference GetField(IEmitScope scope)
    {
        if (_field != null)
        {
            return _field;
        }

        _field = scope.ResolveGlobalField(_name);
        return _field;
    }
}

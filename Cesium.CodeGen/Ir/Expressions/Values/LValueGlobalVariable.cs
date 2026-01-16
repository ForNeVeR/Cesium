// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueGlobalVariable : LValueField
{
    private FieldReference? _field;

    public LValueGlobalVariable(IType type, string name)
    {
        Type = type;
        Name = name;
    }

    public string Name { get; }

    internal IType Type { get; }

    public override IType GetValueType() => Type;

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

        _field = scope.ResolveGlobalField(Name);
        return _field;
    }
}

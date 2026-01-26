// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class CharConstant : IConstant
{
    public CharConstant(char value)
    {
        Value = checked((byte)value);
    }

    public byte Value { get; }

    public void EmitTo(IEmitScope scope)
    {
        var instructions = scope.Method.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)Value));
    }

    public IType GetConstantType() => CTypeSystem.Int;

    public override string ToString() => $"char: {Value}";
}

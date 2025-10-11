// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class FloatingPointConstant : IConstant
{
    public FloatingPointConstant(double value, bool isFloat)
    {
        Value = value;
        IsFloat = isFloat;
    }

    public double Value { get; }

    public bool IsFloat { get; }

    public void EmitTo(IEmitScope scope)
    {
        if (IsFloat)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, (float)Value));
        }
        else
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, Value));
        }
    }

    public IType GetConstantType() => IsFloat ? CTypeSystem.Float : CTypeSystem.Double;

    public override string ToString() => $"{(IsFloat ? "float" : "double")}: {Value}";
}

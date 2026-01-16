// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class StringConstant : IConstant
{
    public StringConstant(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public void EmitTo(IEmitScope scope)
    {
        var fieldReference = scope.AssemblyContext.GetConstantPoolReference(Value);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
    }

    public IType GetConstantType() => CTypeSystem.CharPtr;
}

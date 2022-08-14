using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class IntegerConstant : IConstant
{
    private readonly int _value;

    public IntegerConstant(string value)
    {
        if (!int.TryParse(value, out _value))
            throw new CesiumCompilationException($"Cannot parse an integer literal: {value}.");
    }

    public void EmitTo(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(_value switch
        {
            0 => Instruction.Create(OpCodes.Ldc_I4_0),
            1 => Instruction.Create(OpCodes.Ldc_I4_1),
            2 => Instruction.Create(OpCodes.Ldc_I4_2),
            3 => Instruction.Create(OpCodes.Ldc_I4_3),
            4 => Instruction.Create(OpCodes.Ldc_I4_4),
            5 => Instruction.Create(OpCodes.Ldc_I4_5),
            6 => Instruction.Create(OpCodes.Ldc_I4_6),
            7 => Instruction.Create(OpCodes.Ldc_I4_7),
            8 => Instruction.Create(OpCodes.Ldc_I4_8),
            -1 => Instruction.Create(OpCodes.Ldc_I4_M1),
            >= sbyte.MinValue and <= sbyte.MaxValue => Instruction.Create(OpCodes.Ldc_I4_S, (sbyte) _value),
            _ => Instruction.Create(OpCodes.Ldc_I4, _value)
        });
    }

    public IType GetConstantType(IDeclarationScope scope) => scope.CTypeSystem.Int;

    public override string ToString() => $"integer: {_value}";
}

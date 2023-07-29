using System.Globalization;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class IntegerConstant : IConstant
{
    public IntegerConstant(string value)
    {
        if (!TryParse(value, out var intValue))
            throw new CompilationException($"Cannot parse an integer literal: {value}.");

        Value = intValue;
    }

    public IntegerConstant(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public void EmitTo(IEmitScope scope)
    {
        scope.Method.Body.Instructions.Add(Value switch
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
            >= sbyte.MinValue and <= sbyte.MaxValue => Instruction.Create(OpCodes.Ldc_I4_S, (sbyte) Value),
            _ => Instruction.Create(OpCodes.Ldc_I4, Value)
        });
    }

    public IType GetConstantType(IDeclarationScope scope) => scope.CTypeSystem.Int;

    public override string ToString() => $"integer: {Value}";

    private static bool TryParse(string text, out int value)
    {
        if (text.StartsWith("0x"))
        {
            if (int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            throw new CompilationException($"Invalid hex number {text}");
        }

        if (text.StartsWith("0"))
        {
            value = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] >= '0' && text[i] <= '7')
                {
                    value = 8 * value + (text[i] - '0');
                }
                else
                {
                    throw new CompilationException($"Invalid octal number {text}");
                }
            }

            return true;
        }

        if (int.TryParse(text, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        throw new CompilationException($"Cannot parse an integer literal: {text}.");
    }
}

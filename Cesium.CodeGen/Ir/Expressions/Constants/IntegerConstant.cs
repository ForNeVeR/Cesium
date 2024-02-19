using System.Globalization;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class IntegerConstant : IConstant
{
    public IntegerConstant(string value)
    {
        if (!TryParse(value, out var intValue))
            throw new CompilationException($"Cannot parse an integer literal: {value}.");

        Value = intValue;
    }

    public IntegerConstant(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public void EmitTo(IEmitScope scope)
    {
        if (Value > int.MaxValue || Value < int.MinValue)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I8, Value));
            return;
        }

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
            _ => Instruction.Create(OpCodes.Ldc_I4, (int)Value)
        });
    }

    public IType GetConstantType() => CTypeSystem.Int;

    public override string ToString() => $"integer: {Value}";

    private static bool TryParse(string text, out long value)
    {
        var textSpan = text.AsSpan();
        if (textSpan.EndsWith("L"))
        {
            textSpan = textSpan[..^1];
        }

        if (textSpan.StartsWith("0x"))
        {
            if (long.TryParse(textSpan[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            throw new CompilationException($"Invalid hex number {text}");
        }

        if (textSpan.StartsWith("0"))
        {
            value = 0;
            for (var i = 0; i < textSpan.Length; i++)
            {
                if (textSpan[i] >= '0' && textSpan[i] <= '7')
                {
                    value = 8 * value + (textSpan[i] - '0');
                }
                else
                {
                    throw new CompilationException($"Invalid octal number {text}");
                }
            }

            return true;
        }

        if (long.TryParse(textSpan, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        throw new CompilationException($"Cannot parse an integer literal: {text}.");
    }
}

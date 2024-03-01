using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class CharConstant : IConstant
{
    public CharConstant(string value)
    {
        Value = checked((byte)UnescapeCharacter(value));
    }

    public byte Value { get; }

    public void EmitTo(IEmitScope scope)
    {
        var instructions = scope.Method.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)Value));
        instructions.Add(Instruction.Create(OpCodes.Conv_U1));
    }

    public IType GetConstantType() => CTypeSystem.Char;

    public override string ToString() => $"char: {Value}";

    private static char UnescapeCharacter(string text)
    {
        text = text.Replace("'", string.Empty);
        if (text.Length == 1) return text[0];
        return text[1] switch
        {
            '\'' => '\'',
            '0' => '\0',
            '"' => '"',
            '\\' => '\\',
            'a' => '\a',
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'v' => '\v',
            'x' => (char)int.Parse(text.AsSpan(2), System.Globalization.NumberStyles.AllowHexSpecifier),
            > '0' and < '9' => (char)Convert.ToInt32(text.Substring(2), 8),
            _ => throw new CompilationException($"Unknown escape sequence '{text}'"),
        };
    }
}

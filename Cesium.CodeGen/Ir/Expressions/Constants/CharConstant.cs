using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

public class CharConstant : IConstant
{
    private readonly byte _value;

    public CharConstant(string value)
    {
        _value = checked((byte)UnescapeCharacter(value));
    }

    public void EmitTo(FunctionScope scope)
    {
        var instructions = scope.Method.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)_value));
        instructions.Add(Instruction.Create(OpCodes.Conv_U1));
    }

    public override string ToString() => $"char: {_value}";

    private static char UnescapeCharacter(string text)
    {
        text = text.Replace("'", string.Empty);
        if (text.Length == 1) return text[0];
        return text[1] switch
        {
            '\'' => '\'',
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
            _ => throw new InvalidOperationException($"Unknown escape sequence '{text}'"),
        };
    }
}

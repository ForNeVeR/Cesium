using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class IntegerConstant : IConstant
{
    private readonly int _value;

    public IntegerConstant(string value)
    {
        if (!int.TryParse(value, out _value))
            throw new NotSupportedException($"Cannot parse an integer literal: {value}.");
    }

    public void EmitTo(IDeclarationScope scope)
    {
        // TODO[#92]: Optimizations like Ldc_I4_0 for selected constants
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, _value));
    }

    public override string ToString() => $"integer: {_value}";
}

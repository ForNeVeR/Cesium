using System.Globalization;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class FloatingPointConstant : IConstant
{
    private readonly double _value;

    public FloatingPointConstant(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out _value))
            throw new CompilationException($"Cannot parse a double literal: {value}.");
    }

    public void EmitTo(IDeclarationScope scope)
    {
        // TODO[#248]: This should support `float` as well.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, _value));
    }

    public IType GetConstantType(IDeclarationScope scope) => scope.CTypeSystem.Double;

    public override string ToString() => $"double: {_value}";
}

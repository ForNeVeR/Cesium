using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Globalization;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class DoubleConstant : IConstant
{
    private readonly double _value;

    public DoubleConstant(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out _value))
            throw new NotSupportedException($"Cannot parse an double literal: {value}.");
    }

    public void EmitTo(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, _value));
    }

    public IType GetConstantType(IDeclarationScope scope) => scope.CTypeSystem.Double;

    public override string ToString() => $"double: {_value}";
}

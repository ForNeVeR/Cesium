using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Globalization;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class FloatConstant : IConstant
{
    private readonly float _value;

    public FloatConstant(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out _value))
            throw new NotSupportedException($"Cannot parse an float literal: {value}.");
    }

    public void EmitTo(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, _value));
    }

    public IType GetConstantType(IDeclarationScope scope) => scope.CTypeSystem.Float;

    public override string ToString() => $"float: {_value}";
}

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class FloatingPointConstant : IConstant
{
    private readonly double _value;
    private readonly bool _isFloat;

    public FloatingPointConstant(double value, bool isFloat)
    {
        _value = value;
        _isFloat = isFloat;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (_isFloat)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, (float)_value));
        }
        else
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, _value));
        }
    }

    public IType GetConstantType() => _isFloat ? CTypeSystem.Float : CTypeSystem.Double;

    public override string ToString() => $"{(_isFloat ? "float" : "double")}: {_value}";
}

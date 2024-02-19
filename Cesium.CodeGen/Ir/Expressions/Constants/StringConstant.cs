using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal sealed class StringConstant : IConstant
{
    private readonly string _value;
    public StringConstant(string value)
    {
        _value = value;
    }

    public void EmitTo(IEmitScope scope)
    {
        var fieldReference = scope.AssemblyContext.GetConstantPoolReference(_value);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
    }

    public IType GetConstantType() => CTypeSystem.CharPtr;
}

using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal class LValueParameter : ILValue
{
    private readonly ParameterDefinition _definition;
    public LValueParameter(ParameterDefinition definition)
    {
        _definition = definition;
    }

    public void EmitGetValue(FunctionScope scope)
    {
        // TODO: Special instructions to emit Ldarg_0 etc.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, _definition));
    }

    public void EmitSetValue(FunctionScope scope)
    {
        // TODO: Special instructions to emit Starg_0 etc.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Starg, _definition));
    }
}

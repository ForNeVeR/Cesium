using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal class LValueLocalVariable : ILValue
{
    private readonly VariableDefinition _definition;
    public LValueLocalVariable(VariableDefinition definition)
    {
        _definition = definition;
    }

    public void EmitGetValue(FunctionScope scope)
    {
        // TODO: Special instructions to emit Ldloc_0 etc.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, _definition));
    }

    public void EmitSetValue(FunctionScope scope) => scope.StLoc(_definition);
}

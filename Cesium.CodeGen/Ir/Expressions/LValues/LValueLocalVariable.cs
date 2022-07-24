using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal class LValueLocalVariable : ILValue
{
    private readonly VariableDefinition _definition;

    public LValueLocalVariable(VariableDefinition definition)
    {
        _definition = definition;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        // TODO[#92]: Special instructions to emit Ldloc_0 etc.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, _definition));
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, _definition));
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        value.EmitTo(scope);

        scope.StLoc(_definition);
    }

    public TypeReference GetValueType() => _definition.VariableType;
}

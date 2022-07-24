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

    public void EmitGetValue(IDeclarationScope scope)
    {
        // TODO[#92]: Special instructions to emit Ldarg_0 etc.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, _definition));
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarga, _definition));
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        value.EmitTo(scope);

        // TODO[#92]: Special instructions to emit Starg_0 etc.
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Starg, _definition));
    }

    public TypeReference GetValueType() => _definition.ParameterType;
}

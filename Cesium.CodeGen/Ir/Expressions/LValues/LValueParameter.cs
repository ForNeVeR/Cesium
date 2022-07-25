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
        scope.Method.Body.Instructions.Add(_definition.Index switch
        {
            0 => Instruction.Create(OpCodes.Ldarg_0),
            1 => Instruction.Create(OpCodes.Ldarg_1),
            2 => Instruction.Create(OpCodes.Ldarg_2),
            3 => Instruction.Create(OpCodes.Ldarg_3),
            <= byte.MaxValue => Instruction.Create(OpCodes.Ldarg_S, _definition),
            _ => Instruction.Create(OpCodes.Ldarg, _definition)
        });
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarga, _definition));
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        value.EmitTo(scope);

        scope.Method.Body.Instructions.Add(_definition.Index switch
        {
            <= byte.MaxValue => Instruction.Create(OpCodes.Starg_S, _definition),
            _ => Instruction.Create(OpCodes.Starg, _definition)
        });
    }

    public TypeReference GetValueType() => _definition.ParameterType;
}

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
        scope.Method.Body.Instructions.Add(_definition.Index switch
        {
            0 => Instruction.Create(OpCodes.Ldloc_0),
            1 => Instruction.Create(OpCodes.Ldloc_1),
            2 => Instruction.Create(OpCodes.Ldloc_2),
            3 => Instruction.Create(OpCodes.Ldloc_3),
            <= sbyte.MaxValue => Instruction.Create(OpCodes.Ldloc_S, _definition),
            _ => Instruction.Create(OpCodes.Ldloc, _definition)
        });
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(
            Instruction.Create(
                _definition.Index <= sbyte.MaxValue
                    ? OpCodes.Ldloca_S
                    : OpCodes.Ldloca,
                _definition
            )
        );
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        value.EmitTo(scope);

        scope.StLoc(_definition);
    }

    public TypeReference GetValueType() => _definition.VariableType;
}

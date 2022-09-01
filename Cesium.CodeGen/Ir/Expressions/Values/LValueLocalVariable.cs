using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueLocalVariable : ILValue
{
    private readonly IType _variableType;
    private readonly string _name;
    private VariableDefinition? _definition;

    public LValueLocalVariable(IType variableType, string name)
    {
        _variableType = variableType;
        _name = name;
    }

    public void EmitGetValue(IEmitScope scope)
    {
        var variable = GetVariableDefinition(scope);
        scope.Method.Body.Instructions.Add(variable.Index switch
        {
            0 => Instruction.Create(OpCodes.Ldloc_0),
            1 => Instruction.Create(OpCodes.Ldloc_1),
            2 => Instruction.Create(OpCodes.Ldloc_2),
            3 => Instruction.Create(OpCodes.Ldloc_3),
            <= sbyte.MaxValue => Instruction.Create(OpCodes.Ldloc_S, _definition),
            _ => Instruction.Create(OpCodes.Ldloc, _definition)
        });
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        var variable = GetVariableDefinition(scope);
        scope.Method.Body.Instructions.Add(
            Instruction.Create(
                variable.Index <= sbyte.MaxValue
                    ? OpCodes.Ldloca_S
                    : OpCodes.Ldloca,
                _definition
            )
        );
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        var variable = GetVariableDefinition(scope);
        value.EmitTo(scope);
        scope.StLoc(variable);
    }

    public IType GetValueType() => _variableType;

    private VariableDefinition GetVariableDefinition(IEmitScope scope)
    {
        if (_definition != null)
        {
            return _definition;
        }

        _definition = scope.ResolveVariable(_name);
        return _definition;
    }
}

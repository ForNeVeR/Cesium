using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueParameter : ILValue
{
    private readonly ParameterInfo _parameterInfo;
    private ParameterDefinition? _definition;
    public LValueParameter(ParameterInfo parameterInfo)
    {
        _parameterInfo = parameterInfo;
    }

    public void EmitGetValue(IEmitScope scope)
    {
        var parameterDefinition = GetParameterDefinition(scope);
        scope.Method.Body.Instructions.Add(parameterDefinition.Index switch
        {
            0 => Instruction.Create(OpCodes.Ldarg_0),
            1 => Instruction.Create(OpCodes.Ldarg_1),
            2 => Instruction.Create(OpCodes.Ldarg_2),
            3 => Instruction.Create(OpCodes.Ldarg_3),
            <= byte.MaxValue => Instruction.Create(OpCodes.Ldarg_S, parameterDefinition),
            _ => Instruction.Create(OpCodes.Ldarg, parameterDefinition)
        });
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        var parameterDefinition = GetParameterDefinition(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarga, parameterDefinition));
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        var parameterDefinition = GetParameterDefinition(scope);
        value.EmitTo(scope);

        scope.Method.Body.Instructions.Add(parameterDefinition.Index switch
        {
            <= byte.MaxValue => Instruction.Create(OpCodes.Starg_S, parameterDefinition),
            _ => Instruction.Create(OpCodes.Starg, parameterDefinition)
        });
    }
    private ParameterDefinition GetParameterDefinition(IEmitScope scope)
    {
        if (_definition != null)
        {
            return _definition;
        }

        var parameterName = _parameterInfo.Name ?? throw new AssertException("Name of parameter does not specified");
        _definition = scope.ResolveParameter(parameterName);
        return _definition;
    }

    public IType GetValueType() => _parameterInfo.Type;
}

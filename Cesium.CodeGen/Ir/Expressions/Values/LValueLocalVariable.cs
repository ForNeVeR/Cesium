// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueLocalVariable : ILValue
{
    private readonly IType _variableType;
    private readonly int _varIndex;
    private VariableDefinition? _definition;

    public int VarIndex => _varIndex;

    public VariableDefinition? Definition { get => _definition; set => _definition = value; }

    public LValueLocalVariable(IType variableType, int varIndex)
    {
        _variableType = variableType;
        _varIndex = varIndex;
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
            <= sbyte.MaxValue => Instruction.Create(OpCodes.Ldloc_S, Definition),
            _ => Instruction.Create(OpCodes.Ldloc, Definition)
        });
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        var variable = GetVariableDefinition(scope);
        if (_variableType is InPlaceArrayType)
        {
            EmitGetValue(scope);
        }
        else
        {
            scope.Method.Body.Instructions.Add(
                Instruction.Create(
                    variable.Index <= sbyte.MaxValue
                        ? OpCodes.Ldloca_S
                        : OpCodes.Ldloca,
                    Definition
                )
            );
        }
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        var variable = GetVariableDefinition(scope);
        value.EmitTo(scope);
        if (value is CompoundInitializationExpression)
        {
            // for compound initialization copy memory.s
            scope.AddInstruction(OpCodes.Ldloc, variable);
            var expression = ((InPlaceArrayType)_variableType).GetSizeInBytesExpression(scope.AssemblyContext.ArchitectureSet);
            expression.EmitTo(scope);
            scope.AddInstruction(OpCodes.Conv_U);

            var initializeCompoundMethod = scope.Context.GetRuntimeHelperMethod("InitializeCompound");
            scope.AddInstruction(OpCodes.Call, initializeCompoundMethod);
        }
        else
        {
            // Regular initialization.
            scope.StLoc(variable);
        }
    }

    public IType GetValueType() => _variableType;

    private VariableDefinition GetVariableDefinition(IEmitScope scope) =>
        Definition ??= scope.ResolveVariable(VarIndex);
}

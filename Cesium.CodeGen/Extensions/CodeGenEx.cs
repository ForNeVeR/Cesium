using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Extensions;

internal static class CodeGenEx
{
    private static void AddInstruction(this IEmitScope scope, Instruction instruction) =>
        scope.Method.Body.Instructions.Add(instruction);
    public static void AddInstruction(this IEmitScope scope, OpCode opCode) =>
        scope.Method.Body.Instructions.Add(Instruction.Create(opCode));
    public static void AddInstruction(this IEmitScope scope, OpCode opCode, int value) =>
        scope.Method.Body.Instructions.Add(Instruction.Create(opCode, value));
    public static void AddInstruction(this IEmitScope scope, OpCode opCode, TypeReference value) =>
        scope.Method.Body.Instructions.Add(Instruction.Create(opCode, value));
    public static void AddInstruction(this IEmitScope scope, OpCode opCode, MethodReference value) =>
        scope.Method.Body.Instructions.Add(Instruction.Create(opCode, value));
    public static void AddInstruction(this IEmitScope scope, OpCode opCode, VariableDefinition value) =>
        scope.Method.Body.Instructions.Add(Instruction.Create(opCode, value));
    public static void AddInstruction(this IEmitScope scope, OpCode opCode, FieldReference value) =>
        scope.Method.Body.Instructions.Add(Instruction.Create(opCode, value));

    public static void StLoc(this IEmitScope scope, VariableDefinition variable)
    {
        scope.AddInstruction(variable.Index switch
        {
            0 => Instruction.Create(OpCodes.Stloc_0),
            1 => Instruction.Create(OpCodes.Stloc_1),
            2 => Instruction.Create(OpCodes.Stloc_2),
            3 => Instruction.Create(OpCodes.Stloc_3),
            <= sbyte.MaxValue => Instruction.Create(OpCodes.Stloc_S, variable),
            _ => Instruction.Create(OpCodes.Stloc, variable)
        });
    }

    public static void LdSFld(this IEmitScope scope, FieldReference field)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Ldsfld, field));
    }

    public static void LdSFldA(this IEmitScope scope, FieldReference field)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Ldsflda, field));
    }

    public static void StSFld(this IEmitScope scope, FieldReference field)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Stsfld, field));
    }

    public static void LdFld(this IEmitScope scope, FieldReference field) =>
        scope.AddInstruction(Instruction.Create(OpCodes.Ldfld, field));

    public static void LdFldA(this IEmitScope scope, FieldReference field) =>
        scope.AddInstruction(Instruction.Create(OpCodes.Ldflda, field));

    public static void StFld(this IEmitScope scope, FieldReference field) =>
        scope.AddInstruction(Instruction.Create(OpCodes.Stfld, field));

    public static void LdFtn(this IEmitScope scope, MethodReference method)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Ldftn, method));
    }

    public static void SizeOf(this IEmitScope scope, TypeReference type) =>
        scope.AddInstruction(Instruction.Create(OpCodes.Sizeof, type));
}

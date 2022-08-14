using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Extensions;

internal static class CodeGenEx
{
    private static void AddInstruction(this IDeclarationScope scope, Instruction instruction) =>
        scope.Method.Body.Instructions.Add(instruction);

    public static void StLoc(this IDeclarationScope scope, VariableDefinition variable)
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

    public static void LdSFld(this IDeclarationScope scope, FieldReference field)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Ldsfld, field));
    }

    public static void LdSFldA(this IDeclarationScope scope, FieldReference field)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Ldsflda, field));
    }

    public static void StSFld(this IDeclarationScope scope, FieldReference field)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Stsfld, field));
    }

    public static void LdFtn(this IDeclarationScope scope, MethodReference method)
    {
        scope.AddInstruction(Instruction.Create(OpCodes.Ldftn, method));
    }
}

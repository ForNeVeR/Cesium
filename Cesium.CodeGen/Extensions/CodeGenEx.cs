using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Extensions;

internal static class CodeGenEx
{
    public static void StLoc(this IDeclarationScope scope, VariableDefinition variable)
    {
        scope.Method.Body.Instructions.Add(variable.Index switch
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
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
    }

    public static void LdSFldA(this IDeclarationScope scope, FieldReference field)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, field));
    }

    public static void StSFld(this IDeclarationScope scope, FieldReference field)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, field));
    }
}

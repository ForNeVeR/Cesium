using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Extensions;

internal static class CodeGenEx
{
    public static void StLoc(this IDeclarationScope scope, VariableDefinition variable)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, variable));
    }
}

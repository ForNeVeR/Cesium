using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Extensions;

public static class CodeGenEx
{
    public static void StLoc(this FunctionScope scope, VariableDefinition variable)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, variable));
    }
}

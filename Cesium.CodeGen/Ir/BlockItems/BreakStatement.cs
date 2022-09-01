using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class BreakStatement : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        if (scope is not ForScope forScope)
            throw new CompilationException("Can't break not from for statement");

        var endInstruction = scope.Method.Body.GetILProcessor().Create(OpCodes.Nop);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, endInstruction));

        forScope.EndInstruction = endInstruction;
    }
}

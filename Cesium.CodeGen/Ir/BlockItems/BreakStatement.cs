using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class BreakStatement : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope)
    {
        var breakLabel = scope.GetBreakLabel();
        if (breakLabel is null)
            throw new CompilationException("Can't break not from for statement");

        return new GoToStatement(breakLabel);
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Break statement should be lowered");

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement) => false;
}

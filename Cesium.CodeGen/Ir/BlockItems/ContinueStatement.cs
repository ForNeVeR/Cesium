using Cesium.CodeGen.Contexts;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ContinueStatement : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope)
    {
        var continueLabel = scope.GetContinueLabel();
        if (continueLabel is null)
            throw new CompilationException("Can't use continue outside of a loop construct.");

        return new GoToStatement(continueLabel);
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Continue statement should be lowered");

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement) => false;
}

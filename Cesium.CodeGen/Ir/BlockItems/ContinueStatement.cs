using Cesium.CodeGen.Contexts;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ContinueStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var continueLabel = scope.GetContinueLabel();
        if (continueLabel is null)
            throw new CompilationException("Can't break not from loop statement");

        return new GoToStatement(continueLabel);
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Continue statement should be lowered");
}

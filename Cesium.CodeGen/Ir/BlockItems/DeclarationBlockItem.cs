using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    public ScopedIdentifierDeclaration Declaration { get; }

    internal DeclarationBlockItem(ScopedIdentifierDeclaration declaration)
    {
        Declaration = declaration;
    }

    public void EmitTo(IEmitScope scope)
    {
        throw new AssertException("Should be lowered");
    }
}

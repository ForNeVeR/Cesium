using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    public ScopedIdentifierDeclaration Declaration { get; }

    internal DeclarationBlockItem(ScopedIdentifierDeclaration declaration)
    {
        Declaration = declaration;
    }
}

using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    bool HasDefiniteReturn => false;

    IBlockItem Lower(IDeclarationScope scope);
    void EmitTo(IDeclarationScope scope);
}

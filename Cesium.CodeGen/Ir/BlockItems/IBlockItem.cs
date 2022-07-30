using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    bool HasDefiniteReturn => false;

    IBlockItem Lower();
    void EmitTo(IDeclarationScope scope);
}

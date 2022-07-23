using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    IBlockItem Lower();
    void EmitTo(IDeclarationScope scope);
}

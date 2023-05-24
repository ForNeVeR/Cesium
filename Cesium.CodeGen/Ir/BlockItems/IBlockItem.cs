using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    IBlockItem Lower(IDeclarationScope scope);
    void EmitTo(IEmitScope scope);
}

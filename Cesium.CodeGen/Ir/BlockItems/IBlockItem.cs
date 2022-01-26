using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    IBlockItem Lower();
    void EmitTo(FunctionScope scope);
}

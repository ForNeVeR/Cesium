using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement);
    IBlockItem Lower(IDeclarationScope scope);
    void EmitTo(IEmitScope scope);
}

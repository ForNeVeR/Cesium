using Cesium.CodeGen.Ir.Declarations;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class TagBlockItem : IBlockItem
{
    public ICollection<LocalDeclarationInfo> Types { get; }

    public TagBlockItem(ICollection<LocalDeclarationInfo> types)
    {
        Types = types;
    }
}

using Cesium.CodeGen.Ir.Declarations;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class TypeDefBlockItem : IBlockItem
{
    public ICollection<LocalDeclarationInfo> Types { get; }

    public TypeDefBlockItem(TypeDefDeclaration declaration)
    {
        Types = declaration.Types;
    }

    public TypeDefBlockItem(ICollection<LocalDeclarationInfo> types)
    {
        Types = types;
    }
}

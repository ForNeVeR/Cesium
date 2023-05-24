using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class TagBlockItem : IBlockItem
{
    public ICollection<LocalDeclarationInfo> Types { get; }

    public TagBlockItem(TypeDefDeclaration declaration)
    {
        Types = declaration.Types;
    }

    public TagBlockItem(ICollection<LocalDeclarationInfo> types)
    {
        Types = types;
    }

    public void EmitTo(IEmitScope scope)
    {
        foreach (var typeDef in Types)
        {
            var (type, identifier, _) = typeDef;
            if (type is IGeneratedType t)
                scope.Context.GenerateType(identifier!, t);
        }
    }
}

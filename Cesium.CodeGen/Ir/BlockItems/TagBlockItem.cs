using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class TagBlockItem : IBlockItem
{
    private readonly ICollection<LocalDeclarationInfo> types;

    public TagBlockItem(TypeDefDeclaration declaration)
    {
        declaration.Deconstruct(out types);
    }

    public TagBlockItem(ICollection<LocalDeclarationInfo> types)
    {
        this.types = types;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        List<LocalDeclarationInfo> list = new List<LocalDeclarationInfo>();
        foreach (var typeDef in types)
        {
            var (type, identifier, cliImportMemberName) = typeDef;
            if (identifier == null)
                throw new CompilationException($"Anonymous typedef not supported: {type}.");

            if (cliImportMemberName != null)
                throw new CompilationException($"typedef for CLI import not supported: {cliImportMemberName}.");

            type = scope.ResolveType(type);
            scope.AddTagDefinition(identifier, type);
            list.Add(new LocalDeclarationInfo(type, identifier, cliImportMemberName));
        }

        return new TagBlockItem(list);
    }

    public void EmitTo(IEmitScope scope)
    {
        foreach (var typeDef in types)
        {
            var (type, identifier, _) = typeDef;
            if (type is IGeneratedType t)
                scope.Context.GenerateType(identifier!, t);
        }
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement) => false;
}

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

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

    public IBlockItem Lower(IDeclarationScope scope)
    {
        List<LocalDeclarationInfo> list = new List<LocalDeclarationInfo>();
        foreach (var typeDef in Types)
        {
            var (type, identifier, cliImportMemberName) = typeDef;
            if (identifier == null)
                throw new CompilationException($"Anonymous typedef not supported: {type}.");

            if (cliImportMemberName != null)
                throw new CompilationException($"typedef for CLI import not supported: {cliImportMemberName}.");

            type = scope.ResolveType(type);
            scope.AddTypeDefinition(identifier, type);

            if (typeDef.Type is StructType { Identifier: { } tag })
            {
                scope.AddTagDefinition(tag, type);
            }

            list.Add(new LocalDeclarationInfo(type, identifier, cliImportMemberName));
        }

        return new TypeDefBlockItem(list);
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

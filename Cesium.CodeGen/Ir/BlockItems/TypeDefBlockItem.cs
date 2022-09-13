using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class TypeDefBlockItem : IBlockItem
{
    private readonly TypeDefDeclaration _declaration;

    public TypeDefBlockItem(TypeDefDeclaration declaration)
    {
        _declaration = declaration;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        _declaration.Deconstruct(out var types);
        foreach (var typeDef in types)
        {
            var (type, identifier, cliImportMemberName) = typeDef;
            if (identifier == null)
                throw new CompilationException($"Anonymous typedef not supported: {type}.");

            if (cliImportMemberName != null)
                throw new CompilationException($"typedef for CLI import not supported: {cliImportMemberName}.");

            scope.AddTypeDefinition(identifier, type);
        }

        return this;
    }

    public void EmitTo(IEmitScope scope)
    {
        _declaration.Deconstruct(out var types);
        foreach (var typeDef in types)
        {
            var (type, identifier, _) = typeDef;
            if (type is IGeneratedType t)
                scope.Context.GenerateType(identifier!, t);
        }
    }
}

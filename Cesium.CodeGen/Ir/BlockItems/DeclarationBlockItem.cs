using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    private readonly ScopedIdentifierDeclaration _declaration;
    internal DeclarationBlockItem(ScopedIdentifierDeclaration declaration)
    {
        _declaration = declaration;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var (storageClass, items) = _declaration;
        var newItems = new List<InitializableDeclarationInfo>();
        foreach (var (declaration, initializer) in items)
        {
            var (type, identifier, cliImportMemberName) = declaration;

            // TODO[#91]: A place to register whether {type} is const or not.

            if (identifier == null)
                throw new CompilationException("An anonymous local declaration isn't supported.");

            if (cliImportMemberName != null)
                throw new CompilationException(
                    $"Local declaration with a CLI import member name {cliImportMemberName} isn't supported.");

            type = scope.ResolveType(type);
            scope.AddVariable(identifier, type);

            var initializerExpression = initializer;
            if (initializerExpression != null)
            {
                var initializerType = initializerExpression.Lower(scope).GetExpressionType(scope);
                if (scope.CTypeSystem.IsConversionAvailable(initializerType, type)
                    && !initializerType.Equals(type))
                {
                    initializerExpression = new TypeCastExpression(type, initializerExpression);
                }
            }

            newItems.Add(new InitializableDeclarationInfo(new LocalDeclarationInfo(type, identifier, cliImportMemberName), initializerExpression?.Lower(scope)));
        }

        return new DeclarationBlockItem(new ScopedIdentifierDeclaration(storageClass, newItems));
    }


    public void EmitTo(IEmitScope scope)
    {
        var (storageClass, declarations) = _declaration;
        if (storageClass != StorageClass.Auto)
            throw new WipException(WipException.ToDo, "Storage class static isn't supported, yet.");

        foreach (var (declaration, initializer) in declarations)
        {
            var (type, identifier, _) = declaration;
            switch (initializer)
            {
                case null when type is not InPlaceArrayType:
                    continue;
                case null when type is InPlaceArrayType arrayType:
                    arrayType.EmitInitializer(scope);
                    break;
                default:
                    initializer?.EmitTo(scope);
                    break;
            }

            var variable = scope.ResolveVariable(identifier!);
            scope.StLoc(variable);
        }
    }
}

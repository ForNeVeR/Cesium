using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
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
            scope.AddVariable(storageClass, identifier, type);

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

            var newDeclaration = new LocalDeclarationInfo(type, identifier, cliImportMemberName);
            var initializableDeclaration = new InitializableDeclarationInfo(newDeclaration, initializerExpression?.Lower(scope));
            newItems.Add(initializableDeclaration);
        }

        return new DeclarationBlockItem(new ScopedIdentifierDeclaration(storageClass, newItems));
    }

    public IEnumerable<IBlockItem> LowerInitializers()
    {
        var (storageClass, items) = _declaration;
        foreach (var (declaration, initializer) in items)
        {
            var (type, identifier, cliImportMemberName) = declaration;
            if (identifier is null)
                throw new CompilationException("An anonymous local declaration isn't supported.");

            var initializableDeclaration = new InitializableDeclarationInfo(declaration, null);
            yield return new DeclarationBlockItem(new ScopedIdentifierDeclaration(storageClass, new[] { initializableDeclaration }));
            if (initializer is not null)
            {
                yield return new ExpressionStatement(new AssignmentExpression(new IdentifierExpression(identifier), BinaryOperator.Assign, initializer));
            }
        }
    }

    public void EmitTo(IEmitScope scope)
    {
        var (storageClass, declarations) = _declaration;
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

            switch (storageClass)
            {
                case StorageClass.Auto:
                    var variable = scope.ResolveVariable(identifier!);
                    scope.StLoc(variable);
                    break;
                case StorageClass.Static:
                    var field = scope.ResolveGlobalField(identifier!);
                    scope.StSFld(field);
                    break;
                default:
                    throw new CompilationException($"Storage class {storageClass} is not supported");
            } 
        }
    }
}

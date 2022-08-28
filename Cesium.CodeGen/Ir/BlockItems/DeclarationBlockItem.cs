using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    private readonly IScopedDeclarationInfo _declaration;
    private DeclarationBlockItem(IScopedDeclarationInfo declaration)
    {
        _declaration = declaration;
    }

    public DeclarationBlockItem(Declaration declaration)
        : this(IScopedDeclarationInfo.Of(declaration))
    {
    }

    public IBlockItem Lower()
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration declaration:
            {
                declaration.Deconstruct(out var items);
                return new DeclarationBlockItem(
                    new ScopedIdentifierDeclaration(
                        items.Select(d =>
                            {
                                var (itemDeclaration, initializer) = d;
                                return new InitializableDeclarationInfo(itemDeclaration, initializer?.Lower());
                            })
                            .ToList()));
            }
            case TypeDefDeclaration: return this;
            default: throw new WipException(212, $"Unknown kind of declaration: {_declaration}.");
        }
    }


    public void EmitTo(IDeclarationScope scope)
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration declaration:
                EmitScopedIdentifier(scope, declaration);
                break;
            case TypeDefDeclaration declaration:
                EmitTypeDef(declaration);
                break;
            default:
                throw new WipException(212, $"Unknown kind of declaration: {_declaration}.");
        }
    }

    private static void EmitScopedIdentifier(IDeclarationScope scope, ScopedIdentifierDeclaration scopedDeclaration)
    {
        scopedDeclaration.Deconstruct(out var declarations);
        foreach (var (declaration, initializer) in declarations)
        {
            var (type, identifier, cliImportMemberName) = declaration;

            // TODO[#91]: A place to register whether {type} is const or not.

            if (identifier == null)
                throw new CompilationException("An anonymous local declaration isn't supported.");

            if (cliImportMemberName != null)
                throw new CompilationException(
                    $"Local declaration with a CLI import member name {cliImportMemberName} isn't supported.");

            scope.AddVariable(identifier, type);

            switch (initializer)
            {
                case null when type is not InPlaceArrayType:
                    continue;
                case null when type is InPlaceArrayType arrayType:
                    arrayType.EmitInitializer(scope);
                    break;
                default:
                    var initializerExpression = initializer;
                    if (initializerExpression != null)
                    {
                        // This should be part of lowering process
                        // But because lowering process does not have access to type-system, I place this bandaid.
                        // also I do think that during lowering process initializer expression should be extracted into separate
                        // AssignmentExpression, so we do not duplicate this conversion logic everywhere.
                        if (scope.CTypeSystem.IsConversionAvailable(initializerExpression.GetExpressionType(scope), type))
                        {
                            initializerExpression = new TypeCastExpression(type, initializerExpression);
                        }
                    }

                    initializerExpression?.EmitTo(scope);
                    break;
            }

            var variable = scope.ResolveVariable(identifier);
            scope.StLoc(variable);
        }
    }

    private static void EmitTypeDef(TypeDefDeclaration declaration) =>
        throw new WipException(214, $"typedef is not supported at block level, yet: {declaration}.");
}

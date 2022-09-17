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
    internal DeclarationBlockItem(IScopedDeclarationInfo declaration)
    {
        _declaration = declaration;
    }

    public DeclarationBlockItem(Declaration declaration)
        : this(IScopedDeclarationInfo.Of(declaration))
    {
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration scopedDeclaration:
            {
                scopedDeclaration.Deconstruct(out var items);
                List<InitializableDeclarationInfo> newItems = new List<InitializableDeclarationInfo>();
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

                return new DeclarationBlockItem(new ScopedIdentifierDeclaration(newItems));
            }
            case TypeDefDeclaration typeDefDeclaration:
                typeDefDeclaration.Deconstruct(out var types);
                var resolvedTypes = types.Select(declaration => new LocalDeclarationInfo(scope.ResolveType(declaration.Type), declaration.Identifier, declaration.CliImportMemberName)).ToList();
                return new TypeDefBlockItem(new TypeDefDeclaration(resolvedTypes)).Lower(scope);
            default: throw new WipException(212, $"Unknown kind of declaration: {_declaration}.");
        }
    }


    public void EmitTo(IEmitScope scope)
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration declaration:
                EmitScopedIdentifier(scope, declaration);
                break;
            case TypeDefDeclaration declaration:
                throw new AssertException("Should be lowered");
            default:
                throw new WipException(212, $"Unknown kind of declaration: {_declaration}.");
        }
    }

    private static void EmitScopedIdentifier(IEmitScope scope, ScopedIdentifierDeclaration scopedDeclaration)
    {
        scopedDeclaration.Deconstruct(out var declarations);
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

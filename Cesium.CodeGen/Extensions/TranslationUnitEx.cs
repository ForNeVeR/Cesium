using Cesium.Ast;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using FunctionDefinition = Cesium.CodeGen.Ir.BlockItems.FunctionDefinition;
using IBlockItem = Cesium.CodeGen.Ir.BlockItems.IBlockItem;

namespace Cesium.CodeGen.Extensions;

internal static class TranslationUnitEx
{
    public static IEnumerable<IBlockItem> ToIntermediate(this TranslationUnit translationUnit) =>
        translationUnit.Declarations.SelectMany(x => (x switch
        {
            Ast.FunctionDefinition func => new IBlockItem[] { new FunctionDefinition(func) },
            Ast.SymbolDeclaration sym => GetTopLevelDeclarations(sym),
            _ => throw new WipException(212, $"Declaration not supported: {x}.")
        }));

    private static IEnumerable<IBlockItem> GetTopLevelDeclarations(Ast.SymbolDeclaration sym)
    {
        sym.Deconstruct(out var xdeclaration);
        var _declaration = IScopedDeclarationInfo.Of(xdeclaration);
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration scopedDeclaration:
                scopedDeclaration.Deconstruct(out var items);
                foreach (var (declaration, initializer) in items)
                {
                    var (type, identifier, cliImportMemberName) = declaration;
                    if (identifier == null)
                        throw new CompilationException($"Unnamed global symbol of type {type} is not supported.");

                    if (type is FunctionType functionType)
                    {
                        if (initializer != null)
                            throw new CompilationException(
                                $"Initializer expression for a function declaration isn't supported: {initializer}.");

                        var functionDeclaration = new FunctionDeclaration(identifier, functionType, cliImportMemberName);
                        yield return functionDeclaration;
                        continue;
                    }

                    if (cliImportMemberName != null)
                    {
                        throw new CompilationException($"CLI initializer should be a function for identifier {identifier}.");
                    }

                    if (type is PrimitiveType || type is InPlaceArrayType) // TODO[#75]: Consider other type categories.
                    {
                        var variable = new VariableDefinition(identifier, type, initializer);
                        yield return variable;
                        continue;
                    }

                    throw new WipException(75, $"Declaration not supported, yet: {declaration}.");
                }
                break;
            case TypeDefDeclaration typeDefDeclaration:
                var typeDefBlockItem = new TypeDefBlockItem(typeDefDeclaration);
                yield return typeDefBlockItem;
                break;
            default:
                throw new WipException(212, $"Unknown kind of declaration: {_declaration}.");
        }
    }
}

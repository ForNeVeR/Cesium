using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Extensions;

internal static class TranslationUnitEx
{
    public static IEnumerable<IBlockItem> ToIntermediate(this Ast.TranslationUnit translationUnit) =>
        translationUnit.Declarations.SelectMany(x => (x switch
        {
            Ast.FunctionDefinition func => [new FunctionDefinition(func)],
            Ast.SymbolDeclaration sym => GetTopLevelDeclarations(sym),
            Ast.PInvokeDeclaration pinvoke => [new PInvokeDefinition(pinvoke.Declaration, pinvoke.Prefix)],
            _ => throw new WipException(212, $"Declaration not supported: {x}.")
        }));

    private static IEnumerable<IBlockItem> GetTopLevelDeclarations(Ast.SymbolDeclaration sym)
    {
        sym.Deconstruct(out var astDeclaration);
        var wholeDeclaration = IScopedDeclarationInfo.Of(astDeclaration);
        switch (wholeDeclaration)
        {
            case ScopedIdentifierDeclaration scopedDeclaration:
                var (storageClass, items) = scopedDeclaration;

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

                        var functionDeclaration = new FunctionDeclaration(identifier, storageClass, functionType, cliImportMemberName);
                        yield return functionDeclaration;
                        continue;
                    }

                    if (cliImportMemberName != null)
                    {
                        throw new CompilationException($"CLI initializer should be a function for identifier {identifier}.");
                    }

                    if (type is PrimitiveType or PointerType or InPlaceArrayType
                        || (type is StructType varStructType && varStructType.Identifier != identifier))
                    {
                        var variable = new GlobalVariableDefinition(storageClass, type, identifier, initializer);
                        yield return variable;
                        continue;
                    }

                    if (type is EnumType enumType)
                    {
                        yield return new TagBlockItem(new[] { declaration });
                        foreach(var d in FindEnumConstants(enumType))
                        {
                            yield return d;
                        }
                        continue;
                    }

                    if (type is StructType structType)
                    {
                        yield return new TagBlockItem(new[] { declaration });
                        continue;
                    }

                    throw new WipException(75, $"Declaration not supported, yet: {declaration}.");
                }
                break;
            case TypeDefDeclaration typeDefDeclaration:
                {
                    var typeDefBlockItem = new TypeDefBlockItem(typeDefDeclaration);
                    yield return typeDefBlockItem;
                    foreach (var declaration in typeDefDeclaration.Types)
                    {
                        var (type, identifier, cliImportMemberName) = declaration;
                        if (type is EnumType enumType)
                        {
                            foreach (var d in FindEnumConstants(enumType))
                            {
                                yield return d;
                            }
                            continue;
                        }
                    }
                }
                break;
            default:
                throw new WipException(212, $"Unknown kind of declaration: {wholeDeclaration}.");
        }
    }

    private static IEnumerable<EnumConstantDefinition> FindEnumConstants(EnumType enumType)
    {
        long currentValue = -1;
        foreach (var enumeratorDeclaration in enumType.Members)
        {
            var enumeratorName = enumeratorDeclaration.Declaration.Identifier ?? throw new CompilationException(
                    $"Enum type {enumType.Identifier} has enumerator without name");
            if (enumeratorDeclaration.Initializer is null)
            {
                currentValue++;
            }
            else
            {
                var constantValue = ConstantEvaluator.GetConstantValue(enumeratorDeclaration.Initializer);
                if (constantValue is not IntegerConstant intConstant)
                {
                    throw new CompilationException(
                        $"Enumerator {enumeratorName} has non-integer initializer");
                }

                currentValue = intConstant.Value;
            }

            var variable = new EnumConstantDefinition(enumeratorName, enumType, new Ir.Expressions.ConstantLiteralExpression(new IntegerConstant(currentValue)));
            yield return variable;
        }
    }
}

// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using ConstantLiteralExpression = Cesium.CodeGen.Ir.Expressions.ConstantLiteralExpression;
using FunctionDefinition = Cesium.Ast.FunctionDefinition;
using IBlockItem = Cesium.CodeGen.Ir.BlockItems.IBlockItem;

namespace Cesium.CodeGen.Extensions;

internal static class TranslationUnitEx
{
    public static IEnumerable<IBlockItem> ToIntermediate(this TranslationUnit translationUnit, IDeclarationScope scope) =>
        translationUnit.Declarations.SelectMany(x => (x switch
        {
            FunctionDefinition func => [new Ir.BlockItems.FunctionDefinition(func, scope)],
            SymbolDeclaration sym => GetTopLevelDeclarations(sym, scope),
            PInvokeDeclaration pinvoke => [new PInvokeDefinition(pinvoke.Declaration, pinvoke.Prefix)],
            _ => throw new WipException(212, $"Declaration not supported: {x}.")
        }));

    private static IEnumerable<IBlockItem> GetTopLevelDeclarations(SymbolDeclaration sym, IDeclarationScope scope)
    {
        sym.Deconstruct(out var astDeclaration);
        foreach (var wholeDeclaration in IScopedDeclarationInfo.Of(astDeclaration, scope))
        {
            switch (wholeDeclaration)
            {
                case ScopedIdentifierDeclaration scopedDeclaration:
                    {
                        var (storageClass, declaration, initializer) = scopedDeclaration;
                        var (type, identifier, cliImportMemberName) = declaration;
                        if (type is EnumType)
                        {
                            if (identifier is not null)
                            {
                                yield return new TagBlockItem([declaration]);
                            }

                            foreach (var d in GetEnumDeclarations(type, scope))
                            {
                                yield return d;
                            }
                            continue;
                        }

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

                        var nonConstType = type.EraseConstType();
                        if (nonConstType is PrimitiveType or PointerType or InPlaceArrayType
                            || (nonConstType is StructType varStructType && varStructType.Identifier != identifier)
                            || nonConstType is NamedType)
                        {
                            var variable = new DeclarationBlockItem(new(storageClass, new(type, identifier, null), initializer));
                            yield return variable;
                            continue;
                        }

                        if (type is StructType structType)
                        {
                            yield return new TagBlockItem([declaration]);
                            foreach (var d in GetEnumDeclarations(type, scope))
                            {
                                yield return d;
                            }
                            continue;
                        }

                        throw new WipException(75, $"Declaration not supported, yet: {declaration}.");
                    }
                case TypeDefDeclaration typeDefDeclaration:
                    {
                        var typeDefBlockItem = new TypeDefBlockItem(typeDefDeclaration);
                        yield return typeDefBlockItem;
                        foreach (var declaration in typeDefDeclaration.Types)
                        {
                            var (type, identifier, cliImportMemberName) = declaration;
                            foreach (var d in GetEnumDeclarations(type, scope))
                            {
                                yield return d;
                            }
                        }
                    }
                    break;
                default:
                    throw new WipException(212, $"Unknown kind of declaration: {wholeDeclaration}.");
            }
        }
    }

    private static IEnumerable<IBlockItem> GetEnumDeclarations(IType type, IDeclarationScope scope)
    {
        if (type is EnumType enumType2)
        {
            foreach (var d in FindEnumConstants(enumType2, scope))
            {
                yield return d;
            }
        }

        if (type is StructType nestedStructType)
        {
            foreach (var nd in GetStructEnums(nestedStructType, scope))
            {
                yield return nd;
            }
        }
    }

    private static IEnumerable<IBlockItem> GetStructEnums(StructType structType, IDeclarationScope scope)
    {
        foreach (var md in structType.Members)
        {
            foreach (var nd in GetEnumDeclarations(md.Type, scope))
            {
                yield return nd;
            }
        }
    }

    public static IEnumerable<EnumConstantDefinition> FindEnumConstants(EnumType enumType, IDeclarationScope scope)
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
                var constantValue = ConstantEvaluator.GetConstantValue(enumeratorDeclaration.Initializer, scope);
                if (constantValue is not IntegerConstant intConstant)
                {
                    throw new CompilationException(
                        $"Enumerator {enumeratorName} has non-integer initializer");
                }

                currentValue = intConstant.Value;
            }

            var variable = new EnumConstantDefinition(enumeratorName, enumType, new ConstantLiteralExpression(new IntegerConstant(currentValue)));
            yield return variable;
        }
    }
}

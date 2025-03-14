// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using ConstantLiteralExpression = Cesium.CodeGen.Ir.Expressions.ConstantLiteralExpression;

namespace Cesium.CodeGen.Ir.Declarations;

/// <summary>
/// A scoped declaration info is either a top-level declaration (at the level of the translation unit), or a block-level
/// declaration. Both of them have their scope, and may declare types (typedef).
/// </summary>
internal interface IScopedDeclarationInfo
{
    public static IScopedDeclarationInfo[] Of(Declaration declaration, IDeclarationScope scope)
    {
        var (specifiers, initDeclarators) = declaration;

        if (specifiers.Length > 0 && specifiers[0] is StorageClassSpecifier { Name: "typedef" })
        {
            if (initDeclarators == null)
                throw new CompilationException($"Symbol declaration has no init declarators: {declaration}.");

            return [TypeDefOf(specifiers.RemoveAt(0), initDeclarators, scope)];
        }

        var (storageClass, declarationSpecifiers) = ExtractStorageClass(specifiers);
        if (declarationSpecifiers.Count > 0 && (declarationSpecifiers[0] is StructOrUnionSpecifier || declarationSpecifiers[0] is EnumSpecifier))
        {
            if (initDeclarators == null)
            {
                return declarationSpecifiers.Select(_ =>
                {
                    var ld = LocalDeclarationInfo.Of(new[] { _ }, (Declarator?)null, null, scope);
                    return new ScopedIdentifierDeclaration(storageClass, ld, null);
                }).ToArray();
            }

            var initializationDeclarators = initDeclarators.Value.SelectMany(id => declarationSpecifiers.Select(_ =>
            {
                var ld = LocalDeclarationInfo.Of(new[] { _ }, id.Declarator, null, scope);
                if (id.Initializer is AssignmentInitializer assignmentInitializer)
                    return new ScopedIdentifierDeclaration(storageClass, ld, ExpressionEx.ToIntermediate(assignmentInitializer.Expression, scope));

                if (id.Initializer is null)
                    return new ScopedIdentifierDeclaration(storageClass, ld, null);

                return new ScopedIdentifierDeclaration(storageClass, LocalDeclarationInfo.Of(new[] { _ }, id.Declarator, id.Initializer, scope), null);
            })).ToArray();
            return initializationDeclarators;
        }

        if (initDeclarators == null)
            throw new CompilationException($"Symbol declaration has no init declarators: {declaration}.");

        return IdentifierOf(specifiers, initDeclarators, scope);
    }

    private static TypeDefDeclaration TypeDefOf(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        IEnumerable<InitDeclarator> initDeclarators,
        IDeclarationScope scope)
    {
        var declarations = initDeclarators.Select(d =>
        {
            var (declarator, initializer) = d;
            if (initializer != null)
                throw new CompilationException($"Initializer is not supported for a typedef.");

            return LocalDeclarationInfo.Of(specifiers, declarator, null, scope);
        }).ToList();

        return new TypeDefDeclaration(declarations);
    }

    private static ScopedIdentifierDeclaration[] IdentifierOf(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        IEnumerable<InitDeclarator> initDeclarators,
        IDeclarationScope scope)
    {
        var (storageClass, declarationSpecifiers) = ExtractStorageClass(specifiers);

        var declarations = initDeclarators
            .Select(id =>
            {
                var (declarator, initializer) = id;
                var declarationInfo = LocalDeclarationInfo.Of(declarationSpecifiers, declarator, initializer, scope);
                var (type, _, _) = declarationInfo;
                var expression = ConvertInitializer(type, initializer, scope);
                return new ScopedIdentifierDeclaration(storageClass, declarationInfo, expression);
            })
            .ToArray();
        return declarations;
    }

    public static IExpression? ConvertInitializer(IType? type, Initializer? initializer, IDeclarationScope scope)
    {
        if (initializer is null)
        {
            return null;
        }

        if (initializer is AssignmentInitializer ai)
        {
            if (ai.Designation != null)
                return new CompoundObjectFieldInitializer(ai, scope);
            else
                return ai.Expression.ToIntermediate(scope);
        }

        if (initializer is ArrayInitializer arrayInitializer)
        {
            if (type is null)
            {
                var expr = arrayInitializer.Initializers.Select(i => ConvertInitializer(null, i, scope)).ToImmutableArray();
                return new CompoundObjectInitializationExpression(expr);
            }

            if (type.TypeKind is TypeKind.Struct or TypeKind.Unresolved)
            {
                var expr = arrayInitializer.Initializers.Select(i => ConvertInitializer(null, i, scope)).ToImmutableArray();
                return new CompoundObjectInitializationExpression(type, expr);
            }

            if (type is PrimitiveType primitiveType)
            {
                if (arrayInitializer.Initializers.Length == 0) return new ConstantLiteralExpression(new IntegerConstant(0));
                if (arrayInitializer.Initializers.Length == 1) return ConvertInitializer(type, arrayInitializer.Initializers[0], scope);
                throw new CompilationException($"Primitive types cannot be initialized using more then one initializer list.");
            }

            if (arrayInitializer.Initializers.Length == 0)
            {
                return null;
            }

            if (type is not InPlaceArrayType inPlaceArrayType)
            {
                throw new CompilationException($"Only in-place array types are supported.");
            }

            var nestedInitializers = arrayInitializer.Initializers.Select(i => ConvertInitializer(inPlaceArrayType.Base, i, scope)).ToImmutableArray();
            var expression = new ArrayInitializerExpression(nestedInitializers);
            return new CompoundInitializationExpression(type, expression);
        }

        throw new WipException(225, $"Object initializer not supported, yet: {initializer}.");
    }

    private static (StorageClass, List<IDeclarationSpecifier>) ExtractStorageClass(
        IEnumerable<IDeclarationSpecifier> specifiers)
    {
        StorageClass? storageClass = null;
        var declarationSpecifiers = new List<IDeclarationSpecifier>();
        foreach (var specifier in specifiers)
        {
            if (specifier is not StorageClassSpecifier scs)
            {
                declarationSpecifiers.Add(specifier);
                continue;
            }

            if (storageClass != null)
                throw new CompilationException(
                    $"Storage class specified twice: already processed {storageClass}, but got {specifier}.");

            storageClass = scs.Name switch
            {
                "static" => StorageClass.Static,
                "extern" => StorageClass.Extern,
                _ => throw new WipException(343, $"Storage class not known, yet: {scs.Name}")
            };
        }

        return (storageClass ?? StorageClass.Auto, declarationSpecifiers);
    }
}

internal record TypeDefDeclaration(ICollection<LocalDeclarationInfo> Types) : IScopedDeclarationInfo;
internal record ScopedIdentifierDeclaration(
    StorageClass StorageClass,
    LocalDeclarationInfo Declaration,
    IExpression? Initializer
) : IScopedDeclarationInfo;

internal record InitializableDeclarationInfo(LocalDeclarationInfo Declaration, IExpression? Initializer);

internal enum StorageClass
{
    Static,
    Auto,
    Extern,
}

using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using System.Collections.Immutable;

namespace Cesium.CodeGen.Ir.Declarations;

/// <summary>
/// A scoped declaration info is either a top-level declaration (at the level of the translation unit), or a block-level
/// declaration. Both of them have their scope, and may declare types (typedef).
/// </summary>
internal interface IScopedDeclarationInfo
{
    public static IScopedDeclarationInfo Of(Declaration declaration)
    {
        var (specifiers, initDeclarators) = declaration;

        if (specifiers.Length > 0 && specifiers[0] is StorageClassSpecifier { Name: "typedef" })
        {
            if (initDeclarators == null)
                throw new CompilationException($"Symbol declaration has no init declarators: {declaration}.");

            return TypeDefOf(specifiers.RemoveAt(0), initDeclarators);
        }

        if (specifiers.Length > 0 && (specifiers[0] is StructOrUnionSpecifier || specifiers[0] is EnumSpecifier))
        {
            if (initDeclarators == null)
            {
                Declarator? declarator = null;
                return new ScopedIdentifierDeclaration(StorageClass.Auto,
                    specifiers.Select(_ =>
                    {
                        var ld = LocalDeclarationInfo.Of(new[] { _ }, declarator);
                        return new InitializableDeclarationInfo(ld, null);
                    }).ToImmutableArray());
            }

            var initializationDeclarators = initDeclarators.Value.SelectMany(id => specifiers.Select(_ =>
            {
                var ld = LocalDeclarationInfo.Of(new[] { _ }, id.Declarator);
                if (id.Initializer is AssignmentInitializer assignmentInitializer)
                    return new InitializableDeclarationInfo(ld, ExpressionEx.ToIntermediate(assignmentInitializer.Expression));

                if (id.Initializer is null)
                    return new InitializableDeclarationInfo(ld, null);

                throw new CompilationException($"Struct initializers are not supported.");
            })).ToImmutableArray();
            return new ScopedIdentifierDeclaration(StorageClass.Auto, initializationDeclarators);
        }

        if (initDeclarators == null)
            throw new CompilationException($"Symbol declaration has no init declarators: {declaration}.");

        return IdentifierOf(specifiers, initDeclarators);
    }

    private static TypeDefDeclaration TypeDefOf(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        IEnumerable<InitDeclarator> initDeclarators)
    {
        var declarations = initDeclarators.Select(d =>
        {
            var (declarator, initializer) = d;
            if (initializer != null)
                throw new CompilationException($"Initializer is not supported for a typedef.");

            return LocalDeclarationInfo.Of(specifiers, declarator);
        }).ToList();

        return new TypeDefDeclaration(declarations);
    }

    private static ScopedIdentifierDeclaration IdentifierOf(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        IEnumerable<InitDeclarator> initDeclarators)
    {
        var (storageClass, declarationSpecifiers) = ExtractStorageClass(specifiers);

        var declarations = initDeclarators
            .Select(id => IdentifierOf(declarationSpecifiers, id))
            .ToList();

        return new ScopedIdentifierDeclaration(storageClass, declarations);
    }

    private static InitializableDeclarationInfo IdentifierOf(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        InitDeclarator initDeclarator)
    {
        var (declarator, initializer) = initDeclarator;
        var declarationInfo = LocalDeclarationInfo.Of(specifiers, declarator, initializer);
        var (type, _, _) = declarationInfo;
        var expression = ConvertInitializer(type, initializer);
        return new InitializableDeclarationInfo(declarationInfo, expression);
    }

    private static IExpression? ConvertInitializer(Types.IType? type, Initializer? initializer)
    {
        if (initializer is null)
        {
            return null;
        }

        if (initializer is AssignmentInitializer ai)
        {
            return ai.Expression.ToIntermediate();
        }

        if (initializer is ArrayInitializer arrayInitializer)
        {
            if (type is null)
            {
                throw new CompilationException($"Type for array initializer unknown.");
            }

            if (type is not InPlaceArrayType inPlaceArrayType)
            {
                throw new CompilationException($"Only in-place array types are supported.");
            }

            var nestedInitializers = arrayInitializer.Initializers.Select(i => ConvertInitializer(inPlaceArrayType.Base, i)).ToImmutableArray();
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
    ICollection<InitializableDeclarationInfo> Items
) : IScopedDeclarationInfo;
internal record InitializableDeclarationInfo(LocalDeclarationInfo Declaration, IExpression? Initializer);

internal enum StorageClass
{
    Static,
    Auto,
    Extern,
}

using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

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
        if (initDeclarators == null)
            throw new CompilationException($"Symbol declaration has no init declarators: {declaration}.");

        if (specifiers.Length > 0 && specifiers[0] is StorageClassSpecifier { Name: "typedef" })
        {
            return TypeDefOf(specifiers.RemoveAt(0), initDeclarators);
        }

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
        var declarations = initDeclarators
            .Select(id => IdentifierOf(specifiers, id))
            .ToList();

        return new ScopedIdentifierDeclaration(declarations);
    }

    private static InitializableDeclarationInfo IdentifierOf(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        InitDeclarator initDeclarator)
    {
        var (declarator, initializer) = initDeclarator;
        var declarationInfo = LocalDeclarationInfo.Of(specifiers, declarator);
        var expression = initializer switch
        {
            null => null,
            AssignmentInitializer ai => ai.Expression.ToIntermediate(),
            _ => throw new WipException(225, $"Object initializer not supported, yet: {initializer}.")
        };
        return new InitializableDeclarationInfo(declarationInfo, expression);
    }
}

internal record TypeDefDeclaration(ICollection<LocalDeclarationInfo> Types) : IScopedDeclarationInfo;
internal record ScopedIdentifierDeclaration(ICollection<InitializableDeclarationInfo> Items) : IScopedDeclarationInfo;
internal record InitializableDeclarationInfo(LocalDeclarationInfo Declaration, IExpression? Initializer);

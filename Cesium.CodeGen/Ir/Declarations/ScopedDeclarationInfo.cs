using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

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
            throw new NotSupportedException($"Symbol declaration has no init declarators: {declaration}.");

        if (specifiers.Length > 0 && specifiers[0] is StorageClassSpecifier { Name: "typedef" })
        {
            return TypeDefDeclaration.Of(specifiers.Skip(1), initDeclarators);
        }

        var initializableDeclarations = initDeclarators
            .Select<InitDeclarator, InitializableDeclarationInfo>(id => Of(specifiers, id))
            .ToList();

        return new ScopedIdentifierDeclaration(initializableDeclarations);
    }

    private static InitializableDeclarationInfo Of(
        IList<IDeclarationSpecifier> specifiers,
        InitDeclarator initDeclarator)
    {
        var (declarator, initializer) = initDeclarator;
        var declarationInfo = LocalDeclarationInfo.Of(specifiers, declarator);
        var expression = initializer switch
        {
            null => null,
            AssignmentInitializer ai => ai.Expression.ToIntermediate(),
            _ => throw new NotImplementedException($"Object initializer not supported, yet: {initializer}.")
        };
        return new InitializableDeclarationInfo(declarationInfo, expression);
    }
}

internal record TypeDefDeclaration : IScopedDeclarationInfo
{
    internal static TypeDefDeclaration Of(
        IEnumerable<IDeclarationSpecifier> specifiers,
        IEnumerable<InitDeclarator> initDeclarators)
    {
        throw new NotSupportedException("typedef not supported, yet.");
    }
}
internal record ScopedIdentifierDeclaration(ICollection<InitializableDeclarationInfo> Items) : IScopedDeclarationInfo;
internal record InitializableDeclarationInfo(LocalDeclarationInfo Declaration, IExpression? Initializer);

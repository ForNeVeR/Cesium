using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir;

internal record InitializableDeclarationInfo(DeclarationInfo Declaration, IExpression? Initializer)
{
    public static IEnumerable<InitializableDeclarationInfo> Of(Declaration declaration)
    {
        var (specifiers, initDeclarators) = declaration;
        if (initDeclarators == null)
            throw new NotSupportedException($"Symbol declaration has no init declarators: {declaration}.");

        return initDeclarators.Select<InitDeclarator, InitializableDeclarationInfo>(id => Of(specifiers, id));
    }

    private static InitializableDeclarationInfo Of(
        IList<IDeclarationSpecifier> specifiers,
        InitDeclarator initDeclarator)
    {
        var (declarator, initializer) = initDeclarator;
        var declarationInfo = DeclarationInfo.Of(specifiers, declarator);
        var expression = initializer switch
        {
            null => null,
            AssignmentInitializer ai => ai.Expression.ToIntermediate(),
            _ => throw new NotImplementedException($"Object initializer not supported, yet: {initializer}.")
        };
        return new InitializableDeclarationInfo(declarationInfo, expression);
    }
}

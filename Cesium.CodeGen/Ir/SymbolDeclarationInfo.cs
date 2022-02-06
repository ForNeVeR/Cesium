using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir;

internal record SymbolDeclarationInfo(DeclarationInfo Declaration, IExpression? Initializer)
{
    public static IEnumerable<SymbolDeclarationInfo> Of(SymbolDeclaration symbol)
    {
        symbol.Deconstruct(out var declaration);
        var (specifiers, initDeclarators) = declaration;
        if (initDeclarators == null)
            throw new NotSupportedException($"Symbol declaration has no init declarators: {symbol}.");

        return initDeclarators.Select<InitDeclarator, SymbolDeclarationInfo>(id => Of(specifiers, id));
    }

    private static SymbolDeclarationInfo Of(
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
        return new SymbolDeclarationInfo(declarationInfo, expression);
    }
}

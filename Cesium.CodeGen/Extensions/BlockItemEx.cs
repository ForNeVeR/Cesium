using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.Core;

namespace Cesium.CodeGen.Extensions;

internal static class BlockItemEx
{
    /// <remarks>
    /// This is a specialization for <see cref="FunctionDefinition"/> to be able to check for
    /// <see cref="CompoundStatement.HasDefiniteReturn"/> without the necessity to cast.
    /// </remarks>
    public static CompoundStatement ToIntermediate(this Ast.CompoundStatement statement) => new(statement);

    public static IBlockItem ToIntermediate(this Ast.IBlockItem blockItem) => blockItem switch
    {
        Ast.Declaration d => ToIntermediate(d),
        Ast.CompoundStatement s => s.ToIntermediate(),
        Ast.LabelStatement s => new LabelStatement(s),
        Ast.ReturnStatement s => new ReturnStatement(s),
        Ast.ExpressionStatement s => new ExpressionStatement(s),
        Ast.IfElseStatement s => new IfElseStatement(s),
        Ast.ForStatement s => new ForStatement(s),
        Ast.BreakStatement => new BreakStatement(),
        Ast.GoToStatement s => new GoToStatement(s),
        Ast.AmbiguousBlockItem a => new AmbiguousBlockItem(a),
        _ => throw new WipException(206, $"Statement not supported, yet: {blockItem}.")
    };

    private static IBlockItem ToIntermediate(Ast.Declaration d)
    {
        switch (IScopedDeclarationInfo.Of(d))
        {
            case ScopedIdentifierDeclaration declaration:
                return new DeclarationBlockItem(declaration);
            case TypeDefDeclaration typeDefDeclaration:
                return new TypeDefBlockItem(typeDefDeclaration);
            default:
                throw new WipException(212, $"Unknown kind of declaration: {d}.");
        }
    }
}

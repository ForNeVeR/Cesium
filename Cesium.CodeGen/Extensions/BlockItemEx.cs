using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.Core;

namespace Cesium.CodeGen.Extensions;

internal static class BlockItemEx
{
    public static IBlockItem ToIntermediate(this Ast.IBlockItem blockItem) => blockItem switch
    {
        Ast.Declaration d => ToIntermediate(d),
        Ast.CompoundStatement s => new CompoundStatement(s),
        Ast.LabelStatement s => new LabelStatement(s),
        Ast.ReturnStatement s => new ReturnStatement(s),
        Ast.ExpressionStatement s => new ExpressionStatement(s),
        Ast.IfElseStatement s => new IfElseStatement(s),
        Ast.ForStatement s => new ForStatement(s),
        Ast.WhileStatement s => new WhileStatement(s),
        Ast.DoWhileStatement s => new DoWhileStatement(s),
        Ast.SwitchStatement s => new SwitchStatement(s),
        Ast.CaseStatement s => new CaseStatement(s),
        Ast.BreakStatement => new BreakStatement(),
        Ast.ContinueStatement => new ContinueStatement(),
        Ast.GoToStatement s => new GoToStatement(s),
        Ast.AmbiguousBlockItem a => new AmbiguousBlockItem(a),
        _ => throw new WipException(206, $"Statement not supported, yet: {blockItem}.")
    };

    private static IBlockItem ToIntermediate(Ast.Declaration d)
    {
        return IScopedDeclarationInfo.Of(d) switch
        {
            ScopedIdentifierDeclaration declaration => new DeclarationBlockItem(declaration),
            TypeDefDeclaration typeDefDeclaration => new TypeDefBlockItem(typeDefDeclaration),
            _ => throw new WipException(212, $"Unknown kind of declaration: {d}."),
        };
    }
}

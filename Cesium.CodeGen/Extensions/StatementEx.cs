using Cesium.CodeGen.Ir.BlockItems;

namespace Cesium.CodeGen.Extensions;

internal static class StatementEx
{
    public static CompoundStatement ToIntermediate(this Ast.CompoundStatement statement) => new(statement);

    public static IBlockItem ToIntermediate(this Ast.IBlockItem blockItem) => blockItem switch
    {
        Ast.Declaration d => new DeclarationBlockItem(d),
        Ast.CompoundStatement s => s.ToIntermediate(),
        Ast.ReturnStatement s => new ReturnStatement(s),
        Ast.ExpressionStatement s => new ExpressionStatement(s),
        _ => throw new NotImplementedException($"Statement not supported, yet: {blockItem}.")
    };
}

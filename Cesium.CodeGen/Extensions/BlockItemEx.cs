using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.TopLevel;

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
        Ast.Declaration d => new DeclarationBlockItem(d),
        Ast.CompoundStatement s => s.ToIntermediate(),
        Ast.ReturnStatement s => new ReturnStatement(s),
        Ast.ExpressionStatement s => new ExpressionStatement(s),
        Ast.IfElseStatement s => new IfElseStatement(s),
        Ast.AmbiguousBlockItem a => new AmbiguousBlockItem(a),
        _ => throw new NotImplementedException($"Statement not supported, yet: {blockItem}.")
    };
}

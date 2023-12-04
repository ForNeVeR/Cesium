using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record CompoundStatement : IBlockItem
{
    public IEmitScope? EmitScope { get; }

    public CompoundStatement(List<IBlockItem> items, IEmitScope? emitScope = null)
    {
        EmitScope = emitScope;
        Statements = items;
    }

    public CompoundStatement(Ast.CompoundStatement statement)
        : this(statement.Block.Select(x => x.ToIntermediate()).ToList(), null)
    {
    }

    internal List<IBlockItem> Statements { get; init; }
}

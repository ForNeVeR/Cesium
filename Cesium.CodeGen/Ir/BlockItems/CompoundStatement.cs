using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record CompoundStatement : IBlockItem
{
    private readonly IEmitScope? _emitScope;

    public CompoundStatement(List<IBlockItem> items, IEmitScope? emitScope = null)
    {
        _emitScope = emitScope;
        Statements = items;
    }

    public CompoundStatement(Ast.CompoundStatement statement)
        : this(statement.Block.Select(x => x.ToIntermediate()).ToList(), null)
    {
    }

    internal List<IBlockItem> Statements { get; init; }

    public void EmitTo(IEmitScope scope)
    {
        var realScope = _emitScope ?? scope;

        foreach (var item in Statements)
        {
            item.EmitTo(realScope);
        }
    }
}

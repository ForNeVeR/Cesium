using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record CompoundStatement : IBlockItem
{
    public IEmitScope? EmitScope { get; }

    public bool InheritScope { get; set; }

    public CompoundStatement(List<IBlockItem> items, IEmitScope? emitScope = null)
    {
        EmitScope = emitScope;
        Statements = items;
    }

    public CompoundStatement(Ast.CompoundStatement statement, IDeclarationScope scope)
        : this(statement.Block.Select(x => x.ToIntermediate(scope)).ToList(), null)
    {
    }

    internal List<IBlockItem> Statements { get; init; }
}

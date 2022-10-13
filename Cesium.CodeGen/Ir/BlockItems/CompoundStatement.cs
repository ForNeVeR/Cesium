using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CompoundStatement : IBlockItem
{
    public CompoundStatement(List<IBlockItem> items)
    {
        Statements = items;
    }

    public CompoundStatement(Ast.CompoundStatement statement)
        : this(statement.Block.Select(x => x.ToIntermediate()).ToList())
    {
    }

    bool IBlockItem.HasDefiniteReturn => Statements.Any(x => x.HasDefiniteReturn);

    internal List<IBlockItem> Statements { get; }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return new CompoundStatement(Statements.Select(blockItem => blockItem.Lower(scope)).ToList());
    }

    public void EmitTo(IEmitScope scope)
    {
        foreach (var item in Statements)
        {
            item.EmitTo(scope);
        }
    }
}

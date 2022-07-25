using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CompoundStatement : IBlockItem
{
    private readonly List<IBlockItem> _items;

    private CompoundStatement(List<IBlockItem> items)
    {
        _items = items;
    }

    public CompoundStatement(Ast.CompoundStatement statement)
        : this(statement.Block.Select(x => x.ToIntermediate()).ToList())
    {
    }

    public bool HasDefiniteReturn =>
        _items.Count > 0
        && !_items.All(x =>!(x as DeclarationBlockItem)?.IsScopedIdentifierWithInitalizer?? false); // TODO[#90]: Better definite return analysis.

    public IBlockItem Lower() => this; // since actual lowering of child items is done on emit, anyway

    public void EmitTo(IDeclarationScope scope)
    {
        foreach (var item in _items)
        {
            item.Lower().EmitTo(scope);
        }
    }
}

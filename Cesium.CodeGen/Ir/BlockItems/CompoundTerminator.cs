using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CompoundTerminator : IBlockItem
{
    private readonly CompoundStatement _parent;

    public CompoundTerminator(CompoundStatement parent)
    {
        _parent = parent;
    }

    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    public IBlockItem Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope) { }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        NextNodes = _parent.NextNodes;
    }
}

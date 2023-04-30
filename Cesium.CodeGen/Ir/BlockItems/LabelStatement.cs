using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class LabelStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    private readonly IBlockItem _expression;
    public string Identifier { get; }

    public LabelStatement(Ast.LabelStatement statement)
    {
        _expression = statement.Body.ToIntermediate();
        Identifier = statement.Identifier;
    }

    private LabelStatement(string identifier, IBlockItem expression)
    {
        Identifier = identifier;
        _expression = expression;
    }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        NextNodes = new List<IBlockItem> { _expression };

        _expression.ResolveNextNodes(root, this);
    }

    public IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        yield return _expression;
    }

    public IBlockItem NextNode(IBlockItem child)
    {
        return Parent!.NextNode(this);
    }

    bool IBlockItem.HasDefiniteReturn => _expression.HasDefiniteReturn;

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        scope.AddLabel(Identifier);
        return new LabelStatement(Identifier, _expression.Lower(scope));
    }

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(Identifier);
        scope.Method.Body.Instructions.Add(instruction);
        _expression.EmitTo(scope);
    }
}

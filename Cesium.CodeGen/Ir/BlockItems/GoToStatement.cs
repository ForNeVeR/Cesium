using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class GoToStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    private readonly string _identifier;

    public GoToStatement(Ast.GoToStatement statement)
    {
        _identifier = statement.Identifier;
    }

    public GoToStatement(string identifier)
    {
        _identifier = identifier;
    }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        var label = FindLabel(root, root, _identifier);
        if (label == null) throw new CompilationException($"Unresolved label \"{_identifier}\"");

        NextNodes = new List<IBlockItem> { label };

        label.ResolveNextNodes(root, this);
    }

    private LabelStatement? FindLabel(IBlockItem root, IBlockItem current, string labelName)
    {
        // todo loop detection

        foreach (var child in current.GetChildren(root))
        {
            if (child is LabelStatement label && label.Identifier == labelName)
            {
                return label;
            }

            if (FindLabel(root, child, labelName) is {} nestedLabel)
            {
                return nestedLabel;
            }
        }

        return null;
    }

    public IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        yield break;
    }

    bool IBlockItem.HasDefiniteReturn => false;

    public IBlockItem Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(_identifier);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instruction));
    }
}

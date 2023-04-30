using System.Diagnostics;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CompoundStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    public CompoundStatement(List<IBlockItem> items)
    {
        Statements = items;
        Terminator = new CompoundTerminator(this);
    }

    public CompoundTerminator Terminator { get; }

    public CompoundStatement(Ast.CompoundStatement statement)
        : this(statement.Block.Select(x => x.ToIntermediate()).ToList())
    {
    }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        var nextNodes = new List<IBlockItem>();

        var lastNodeExited = false;

        foreach (var statement in Statements)
        {
            statement.ResolveNextNodes(root, this);

            var finalNodes = statement.FinalNodes(NextNode(statement)).ToList();

            switch (finalNodes.Count)
            {
                case 0:
                    nextNodes.Add(statement);
                    break;

                default:
                    foreach (var node in finalNodes)
                    {
                        var isLastNode = Statements.Last() == statement;

                        if (!Statements.Contains(node) && node != Terminator)
                        {
                            nextNodes.Add(node);
                            lastNodeExited |= isLastNode;
                        }
                        else if (node is ReturnStatement && Statements.Last() == statement)
                        {
                            nextNodes.Add(node);
                            lastNodeExited |= isLastNode;
                        }
                    }

                    break;
            }
        }

        if (!lastNodeExited && parent != this)
        {
            nextNodes.Add(parent.NextNode(this));
        }

        NextNodes = nextNodes;
        Terminator.NextNodes = nextNodes;
    }

    public IBlockItem NextNode(IBlockItem child)
    {
        var childIndex = Statements.IndexOf(child);
        Debug.Assert(childIndex != -1);

        if (Statements.Count > childIndex + 1)
        {
            return Statements[childIndex + 1];
        }
        else
        {
            return Terminator;
        }
    }

    public IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        return Statements;
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

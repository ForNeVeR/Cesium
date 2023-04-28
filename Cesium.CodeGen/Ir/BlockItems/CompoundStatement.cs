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
        var newNestedStatements = new List<IBlockItem>();
        foreach (var blockItem in Statements)
        {
            if (blockItem is DeclarationBlockItem declaration)
            {
                foreach (var splittedBlockItem in declaration.LowerInitializers())
                {
                    newNestedStatements.Add(splittedBlockItem.Lower(scope));
                }
            }
            else
            {
                newNestedStatements.Add(blockItem.Lower(scope));
            }
        }

        return new CompoundStatement(newNestedStatements);
    }

    public void EmitTo(IEmitScope scope)
    {
        foreach (var item in Statements)
        {
            item.EmitTo(scope);
        }
    }
}

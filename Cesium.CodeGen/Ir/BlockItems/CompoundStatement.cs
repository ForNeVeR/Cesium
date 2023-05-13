using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CompoundStatement : IBlockItem
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

    internal List<IBlockItem> Statements { get; }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var blockScope = new BlockScope((IEmitScope) scope, null, null);

        var newNestedStatements = new List<IBlockItem>();
        foreach (var blockItem in Statements)
        {
            newNestedStatements.Add(blockItem.Lower(blockScope));
        }

        return new CompoundStatement(newNestedStatements, blockScope);
    }

    public void EmitTo(IEmitScope scope)
    {
        var realScope = _emitScope ?? scope;

        foreach (var item in Statements)
        {
            item.EmitTo(realScope);
        }
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement)
    {
        var index = Statements.IndexOf(original);
        if (index >= 0)
        {
            Statements[index] = replacement;
            return true;
        }

        foreach (var stmt in Statements)
        {
            if (stmt.TryUnsafeSubstitute(original, replacement))
                return true;
        }

        return false;
    }
}

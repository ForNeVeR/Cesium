using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record LabelStatement : IBlockItem
{
    public IBlockItem Expression { get; init; }
    public bool DidLowered { get; }
    public string Identifier { get; }

    public LabelStatement(Ast.LabelStatement statement)
    {
        Expression = statement.Body.ToIntermediate();
        Identifier = statement.Identifier;
    }

    public LabelStatement(string identifier, IBlockItem expression, bool didLowered = false)
    {
        Identifier = identifier;
        Expression = expression;
        DidLowered = didLowered;
    }
}

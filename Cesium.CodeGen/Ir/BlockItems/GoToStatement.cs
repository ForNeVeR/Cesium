namespace Cesium.CodeGen.Ir.BlockItems;

internal class GoToStatement : IBlockItem
{
    public string Identifier { get; }

    public GoToStatement(Ast.GoToStatement statement)
    {
        Identifier = statement.Identifier;
    }

    public GoToStatement(string identifier)
    {
        Identifier = identifier;
    }
}

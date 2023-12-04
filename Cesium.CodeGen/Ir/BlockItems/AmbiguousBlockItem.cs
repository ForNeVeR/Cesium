namespace Cesium.CodeGen.Ir.BlockItems;

/// <summary>
/// This is a special block item which was constructed in an ambiguous context: it is either a declaration or a function
/// call, depending on the context.
///
/// It defines an AST of form <code>item1(item2);</code>, where item1 is either a function name or a type, and item2 is
/// either a variable name or an argument name.
/// </summary>
internal sealed class AmbiguousBlockItem : IBlockItem
{
    public string Item1 { get; }
    public string Item2 { get; }

    public AmbiguousBlockItem(Ast.AmbiguousBlockItem item)
    {
        (Item1, Item2) = item;
    }
}

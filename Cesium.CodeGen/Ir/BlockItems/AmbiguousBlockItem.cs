using System.Collections.Immutable;
using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.CodeGen.Ir.BlockItems;

/// <summary>
/// This is a special block item which was constructed in an ambiguous context: it is either a declaration or a function
/// call, depending on the context.
///
/// It defines an AST of form <code>item1(item2);</code>, where item1 is either a function name or a type, and item2 is
/// either a variable name or an argument name.
/// </summary>
internal class AmbiguousBlockItem : IBlockItem
{
    public string Item1 { get; }
    public string Item2 { get; }

    public AmbiguousBlockItem(Ast.AmbiguousBlockItem item)
    {
        (Item1, Item2) = item;
    }

    public void EmitTo(IEmitScope scope)
    {
        throw new WipException(213, "Ambiguous variable declarations aren't supported, yet.");
    }
}

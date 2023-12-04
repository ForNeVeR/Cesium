using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class SwitchStatement : IBlockItem
{
    public IExpression Expression { get; }
    public IBlockItem Body { get; }

    public SwitchStatement(Ast.SwitchStatement statement)
    {
        var (expression, body) = statement;

        Expression = expression.ToIntermediate();
        Body = body.ToIntermediate();
    }
}

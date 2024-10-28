using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class ExpressionStatement : IBlockItem
{
    public IExpression? Expression { get; }

    internal ExpressionStatement(IExpression? expression)
    {
        Expression = expression switch
        {
            PostfixIncrementDecrementExpression => new DiscardResultExpression(expression),
            _ => expression
        };
    }

    public ExpressionStatement(Ast.ExpressionStatement statement) : this(statement.Expression?.ToIntermediate())
    {
    }
}

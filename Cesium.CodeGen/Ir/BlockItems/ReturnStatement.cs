using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class ReturnStatement : IBlockItem
{
    public IExpression? Expression { get; }

    public ReturnStatement(Ast.ReturnStatement statement)
    {
        Expression = statement.Expression?.ToIntermediate();
    }

    public ReturnStatement(IExpression? expression)
    {
        Expression = expression;
    }
}

using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DoWhileStatement : IBlockItem
{
    public IExpression TestExpression { get; }
    public IBlockItem Body { get; }

    public DoWhileStatement(Ast.DoWhileStatement statement)
    {
        var (testExpression, body) = statement;

        TestExpression = testExpression.ToIntermediate();
        Body = body.ToIntermediate();
    }
}

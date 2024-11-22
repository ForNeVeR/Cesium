using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class DoWhileStatement : IBlockItem
{
    public IExpression TestExpression { get; }
    public IBlockItem Body { get; }

    public DoWhileStatement(Ast.DoWhileStatement statement, IDeclarationScope scope)
    {
        var (testExpression, body) = statement;

        TestExpression = testExpression.ToIntermediate(scope);
        Body = body.ToIntermediate(scope);
    }
}

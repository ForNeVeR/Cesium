using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class WhileStatement : IBlockItem
{
    public IExpression TestExpression { get; }
    public IBlockItem Body { get; }

    public WhileStatement(Ast.WhileStatement statement, IDeclarationScope scope)
    {
        var (testExpression, body) = statement;

        TestExpression = testExpression.ToIntermediate(scope);
        Body = body.ToIntermediate(scope);
    }
}

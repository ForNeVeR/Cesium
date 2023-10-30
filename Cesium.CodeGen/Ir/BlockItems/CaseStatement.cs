using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class CaseStatement : IBlockItem
{
    public CaseStatement(Ast.CaseStatement statement)
    {
        var (constant, body) = statement;

        Expression = constant?.ToIntermediate();
        Statement = body.ToIntermediate();
    }

    public string Label { get; } = Guid.NewGuid().ToString();
    public IBlockItem Statement { get; }
    public IExpression? Expression { get; }
}

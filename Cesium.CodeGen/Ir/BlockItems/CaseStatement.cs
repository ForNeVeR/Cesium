using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CaseStatement : IBlockItem
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

    public void EmitTo(IEmitScope scope)
    {
        throw new AssertException("Cannot emit case statement independently.");
    }
}

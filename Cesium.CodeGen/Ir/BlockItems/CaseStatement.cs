using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CaseStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    public CaseStatement(IExpression? expression, IBlockItem statement)
    {
        Expression = expression;
        Statement = statement;
    }

    public CaseStatement(Ast.CaseStatement statement)
    {
        var (constant, body) = statement;

        Expression = constant?.ToIntermediate();
        Statement = body.ToIntermediate();
    }

    bool IBlockItem.HasDefiniteReturn => Statement.HasDefiniteReturn;

    internal IBlockItem Statement { get; }
    internal IExpression? Expression { get; }

    public IBlockItem Lower(IDeclarationScope scope) => new CaseStatement(Expression?.Lower(scope), Statement.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        throw new AssertException("Cannot emit case statement independently.");
    }
}

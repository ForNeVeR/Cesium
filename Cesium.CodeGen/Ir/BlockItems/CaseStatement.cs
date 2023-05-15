using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CaseStatement : IBlockItem
{
    private string _label = Guid.NewGuid().ToString();

    private CaseStatement(IExpression? expression, IBlockItem statement, string label)
    {
        _label = label;
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

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // todo: optimize multiple cases at once

        if (scope is not SwitchScope sws)
            throw new AssertException("Cannot use case statement outside of switch");

        sws.SwitchCases.Add(new SwitchCase(Expression, _label));

        return new LabelStatement(_label, Statement).Lower(scope);
    }

    public void EmitTo(IEmitScope scope)
    {
        throw new AssertException("Cannot emit case statement independently.");
    }
}

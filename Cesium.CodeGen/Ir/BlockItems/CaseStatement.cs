using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class CaseStatement : IBlockItem
{
    private readonly string _label = Guid.NewGuid().ToString();

    public CaseStatement(Ast.CaseStatement statement)
    {
        var (constant, body) = statement;

        Expression = constant?.ToIntermediate();
        Statement = body.ToIntermediate();
    }

    bool IBlockItem.HasDefiniteReturn => Statement.HasDefiniteReturn;

    private IBlockItem Statement { get; }
    private IExpression? Expression { get; }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // TODO[#408]: optimize multiple cases at once

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

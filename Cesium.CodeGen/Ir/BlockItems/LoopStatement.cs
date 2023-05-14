using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal abstract class LoopStatement : IBlockItem
{
    protected IBlockItem MakeLoop(
        LoopScope scope,
        IBlockItem? initializer,
        IExpression? testExpression,
        IExpression? updateExpression,
        IBlockItem body,
        string breakLabel,
        string? testConditionLabel,
        string? loopBodyLabel,
        string? updateLabel
    )
    {
        var stmts = new List<IBlockItem>();

        if (initializer != null)
            stmts.Add(initializer);

        testConditionLabel ??= new Guid().ToString();

        stmts.Add(new LabelStatement(testConditionLabel, new ExpressionStatement((IExpression?) null)));

        if (testExpression != null)
        {
            stmts.Add(new IfElseStatement(new UnaryOperatorExpression(UnaryOperator.LogicalNot, testExpression), new GoToStatement(breakLabel), null));
        }

        if (loopBodyLabel != null)
            stmts.Add(new LabelStatement(loopBodyLabel, body));
        else
            stmts.Add(body);

        var updateStmt = new ExpressionStatement(updateExpression);

        if (updateLabel != null)
            stmts.Add(new LabelStatement(updateLabel, updateStmt));
        else
            stmts.Add(updateStmt);

        stmts.Add(new GoToStatement(testConditionLabel));
        stmts.Add(new LabelStatement(breakLabel, new ExpressionStatement((IExpression?) null)));

        return new CompoundStatement(stmts, scope).Lower(scope);
    }

    public abstract IBlockItem Lower(IDeclarationScope scope);

    public void EmitTo(IEmitScope scope)
    {
        throw new CompilationException("Should be lowered");
    }
}

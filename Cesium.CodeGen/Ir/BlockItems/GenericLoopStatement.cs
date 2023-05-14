using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record GenericLoopStatement(
    LoopScope Scope,
    IBlockItem? Initializer,
    IExpression? TestExpression,
    IExpression? UpdateExpression,
    IBlockItem Body,
    string BreakLabel,
    string TestConditionLabel,
    string? LoopBodyLabel,
    string? UpdateLabel
) : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope)
    {
        var stmts = new List<IBlockItem>();

        if (Initializer != null)
            stmts.Add(Initializer);

        stmts.Add(new LabelStatement(TestConditionLabel, new ExpressionStatement((IExpression?) null)));

        if (TestExpression != null)
        {
            stmts.Add(new IfElseStatement(new UnaryOperatorExpression(UnaryOperator.LogicalNot, TestExpression), new GoToStatement(BreakLabel), null));
        }

        if (LoopBodyLabel != null)
            stmts.Add(new LabelStatement(LoopBodyLabel, Body));
        else
            stmts.Add(Body);

        var updateStmt = new ExpressionStatement(UpdateExpression);

        if (UpdateLabel != null)
            stmts.Add(new LabelStatement(UpdateLabel, updateStmt));
        else
            stmts.Add(updateStmt);

        stmts.Add(new GoToStatement(TestConditionLabel));
        stmts.Add(new LabelStatement(BreakLabel, new ExpressionStatement((IExpression?) null)));

        return new CompoundStatement(stmts, scope as IEmitScope).Lower(scope);
    }

    bool IBlockItem.HasDefiniteReturn => Body.HasDefiniteReturn;

    public void EmitTo(IEmitScope unused)
    {
        throw new CompilationException("Should be lowered");
    }
}

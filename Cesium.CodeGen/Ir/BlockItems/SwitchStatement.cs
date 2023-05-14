using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class SwitchStatement : IBlockItem
{
    private record struct MatchGroup(
        IExpression? TestExpression,
        List<IBlockItem> Statements,
        string Label
    );

    private enum SwitchControlState { Preamble, Cases, Body }

    private readonly IExpression _expression;
    private readonly CompoundStatement _body;

    public SwitchStatement(Ast.SwitchStatement statement)
    {
        var (expression, body) = statement;

        _expression = expression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var switchScope = new SwitchScope((IEmitScope)scope);

        var preamble = new List<IBlockItem>();
        var matchGroups = new List<MatchGroup>();

        var currentMatchGroup = new MatchGroup(null, new List<IBlockItem>(), "match-" + Guid.NewGuid());
        var state = SwitchControlState.Preamble;

        void AppendStatement(IBlockItem stmt)
        {
            if (stmt is CaseStatement aCase)
            {
                var comparison = aCase.Expression == null ? null : new ComparisonBinaryOperatorExpression(_expression, BinaryOperator.EqualTo, aCase.Expression);

                if (state == SwitchControlState.Preamble)
                {
                    currentMatchGroup.TestExpression = comparison;
                }
                else if (state == SwitchControlState.Body)
                {
                    matchGroups.Add(currentMatchGroup);
                    currentMatchGroup = new MatchGroup(comparison, new List<IBlockItem>(), "match-" + Guid.NewGuid());
                }
                else
                {
                    if (currentMatchGroup.TestExpression != null && comparison != null)
                    {
                        currentMatchGroup.TestExpression =
                            new LogicalBinaryOperatorExpression(currentMatchGroup.TestExpression, BinaryOperator.LogicalOr, comparison);
                    }
                }

                state = SwitchControlState.Cases;
                AppendStatement(aCase.Statement);
            }
            else if (state == SwitchControlState.Preamble)
            {
                preamble.Add(stmt);
            }
            else
            {
                state = SwitchControlState.Body;
                currentMatchGroup.Statements.Add(stmt);
            }
        }

        foreach (var stmt in _body.Statements)
        {
            AppendStatement(stmt);
        }

        if (currentMatchGroup.Statements.Count > 0)
            matchGroups.Add(currentMatchGroup);


        var targetStmts = new List<IBlockItem>();

        foreach (var matchGroup in matchGroups)
        {
            if (matchGroup.TestExpression != null)
            {
                targetStmts.Add(
                    new IfElseStatement(
                        matchGroup.TestExpression,
                        new GoToStatement(matchGroup.Label),
                        null
                    )
                );
            }
            else
            {
                targetStmts.Add(new GoToStatement(matchGroup.Label));
            }
        }

        foreach (var bodyStmt in preamble)
        {
            targetStmts.Add(bodyStmt);
        }

        foreach (var matchGroup in matchGroups)
        {
            targetStmts.Add(new LabelStatement(matchGroup.Label,  new ExpressionStatement((IExpression?) null)));

            foreach (var bodyStmt in matchGroup.Statements)
            {
                targetStmts.Add(bodyStmt);
            }
        }

        targetStmts.Add(new LabelStatement(switchScope.GetBreakLabel(),  new ExpressionStatement((IExpression?) null)));

        return new CompoundStatement(targetStmts).Lower(switchScope);
    }

    bool IBlockItem.HasDefiniteReturn => ((IBlockItem)_body).HasDefiniteReturn;

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");
}

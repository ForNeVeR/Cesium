using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class SwitchStatement : IBlockItem
{
    public IExpression Expression { get; }
    public IBlockItem Body { get; }

    public SwitchStatement(Ast.SwitchStatement statement)
    {
        var (expression, body) = statement;

        Expression = expression.ToIntermediate();
        Body = body.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var switchCases = new List<SwitchCase>();
        var breakLabel = Guid.NewGuid().ToString();
        var switchScope = new BlockScope((IEmitScope)scope, breakLabel, null, switchCases);

        var loweredBody = Body.Lower(switchScope);
        var targetStmts = new List<IBlockItem>();

        if (switchCases.Count == 0)
        {
            return new ExpressionStatement(Expression).Lower(switchScope);
        }

        var dbi = new DeclarationBlockItem(
            new ScopedIdentifierDeclaration(
                StorageClass.Auto,
                new List<InitializableDeclarationInfo>
                {
                    new(new LocalDeclarationInfo(Expression.GetExpressionType(scope), "$switch_tmp", null),
                        Expression)
                }));

        targetStmts.Add(dbi.Lower(switchScope));

        var idExpr = new IdentifierExpression("$switch_tmp");

        var hasDefaultCase = false;

        foreach (var matchGroup in switchCases)
        {
            if (matchGroup.TestExpression != null)
            {
                targetStmts.Add(
                    new IfElseStatement(
                        new ComparisonBinaryOperatorExpression(idExpr, BinaryOperator.EqualTo, matchGroup.TestExpression).Lower(switchScope),
                        new GoToStatement(matchGroup.Label),
                        null
                    )
                );
            }
            else
            {
                hasDefaultCase = true;
                targetStmts.Add(new GoToStatement(matchGroup.Label));
            }
        }

        if (!hasDefaultCase)
            targetStmts.Add(new GoToStatement(breakLabel));

        targetStmts.Add(loweredBody);
        targetStmts.Add(new LabelStatement(breakLabel,  new ExpressionStatement((IExpression?) null)).Lower(switchScope));

        // avoiding lowering twice
        return new CompoundStatement(targetStmts, switchScope);
    }

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");
}

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class SwitchStatement : IBlockItem
{
    private readonly IExpression _expression;
    private readonly IBlockItem _body;

    public SwitchStatement(Ast.SwitchStatement statement)
    {
        var (expression, body) = statement;

        _expression = expression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var switchScope = new SwitchScope((IEmitScope)scope);

        var loweredBody = _body.Lower(switchScope);
        var targetStmts = new List<IBlockItem>();

        var dbi = new DeclarationBlockItem(
            new ScopedIdentifierDeclaration(
                StorageClass.Auto,
                new List<InitializableDeclarationInfo>
                {
                    new(new LocalDeclarationInfo(_expression.GetExpressionType(scope), "$switch_tmp", null),
                        _expression)
                }));

        foreach (var dbiBlock in dbi.LowerInitializers())
        {
            targetStmts.Add(dbiBlock.Lower(switchScope));
        }

        var idExpr = new IdentifierExpression("$switch_tmp");

        foreach (var matchGroup in switchScope.SwitchCases)
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
                targetStmts.Add(new GoToStatement(matchGroup.Label));
            }
        }

        targetStmts.Add(loweredBody);
        targetStmts.Add(new LabelStatement(switchScope.GetBreakLabel(),  new ExpressionStatement((IExpression?) null)).Lower(switchScope));

        // avoiding lowering twice
        return new CompoundStatement(targetStmts, switchScope);
    }

    bool IBlockItem.HasDefiniteReturn => ((IBlockItem)_body).HasDefiniteReturn;

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");
}

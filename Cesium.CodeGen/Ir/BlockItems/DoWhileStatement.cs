using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DoWhileStatement : LoopStatement, IBlockItem
{
    private readonly IExpression _testExpression;
    private readonly IBlockItem _body;

    public DoWhileStatement(Ast.DoWhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    public override IBlockItem Lower(IDeclarationScope scope)
    {
        var loopScope = new LoopScope((IEmitScope)scope);
        var breakLabel = loopScope.GetBreakLabel();
        var continueLabel = loopScope.GetContinueLabel();

        return MakeLoop(
            loopScope,
            new GoToStatement(continueLabel),
            _testExpression,
            null,
            _body,
            breakLabel,
            null,
            continueLabel,
            null
        );
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;
}

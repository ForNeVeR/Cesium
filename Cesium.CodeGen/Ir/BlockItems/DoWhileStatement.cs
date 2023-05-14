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
        var breakLabel = Guid.NewGuid().ToString();
        var continueLabel = Guid.NewGuid().ToString();

        var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

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

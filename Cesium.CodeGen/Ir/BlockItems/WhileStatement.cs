using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class WhileStatement : LoopStatement, IBlockItem
{
    private readonly IExpression _testExpression;
    private readonly IBlockItem _body;

    public WhileStatement(Ast.WhileStatement statement)
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
            null,
            _testExpression,
            null,
            _body,
            breakLabel,
            continueLabel,
            null,
            null
        );
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;
}

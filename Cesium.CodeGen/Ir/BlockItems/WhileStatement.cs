using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class WhileStatement : LoopStatement, IBlockItem
{
    public IExpression TestExpression { get; }
    public IBlockItem Body { get; }

    public WhileStatement(Ast.WhileStatement statement)
    {
        var (testExpression, body) = statement;

        TestExpression = testExpression.ToIntermediate();
        Body = body.ToIntermediate();
    }

    public override IBlockItem Lower(IDeclarationScope scope)
    {
        var breakLabel = Guid.NewGuid().ToString();
        var continueLabel = Guid.NewGuid().ToString();

        var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

        return MakeLoop(
            loopScope,
            null,
            TestExpression,
            null,
            Body,
            breakLabel,
            continueLabel,
            null,
            null
        );
    }
}

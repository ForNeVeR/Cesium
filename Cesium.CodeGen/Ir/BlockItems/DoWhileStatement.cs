using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DoWhileStatement : LoopStatement, IBlockItem
{
    public IExpression TestExpression { get; }
    public IBlockItem Body { get; }

    public DoWhileStatement(Ast.DoWhileStatement statement)
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
            new GoToStatement(continueLabel),
            TestExpression,
            null,
            Body,
            breakLabel,
            null,
            continueLabel,
            null
        );
    }
}

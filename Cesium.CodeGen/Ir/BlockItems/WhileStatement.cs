using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class WhileStatement : LoopStatement, IBlockItem
{
    private readonly IExpression _testExpression;
    public IBlockItem Body { get; set; }

    public WhileStatement(Ast.WhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
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
            _testExpression,
            null,
            Body,
            breakLabel,
            continueLabel,
            null,
            null
        );
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement)
    {
        if (Body == original)
        {
            Body = replacement;
            return true;
        }

        return Body.TryUnsafeSubstitute(original, replacement);
    }
}

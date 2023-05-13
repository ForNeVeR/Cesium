using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DoWhileStatement : LoopStatement, IBlockItem
{
    private readonly IExpression _testExpression;
    public IBlockItem Body { get; set; }

    public DoWhileStatement(Ast.DoWhileStatement statement)
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
            new GoToStatement(continueLabel),
            _testExpression,
            null,
            Body,
            breakLabel,
            null,
            continueLabel,
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

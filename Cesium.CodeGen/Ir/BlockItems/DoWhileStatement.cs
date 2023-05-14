using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DoWhileStatement : IBlockItem
{
    private readonly IExpression _testExpression;
    private readonly IBlockItem _body;

    public DoWhileStatement(Ast.DoWhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var loopScope = new LoopScope((IEmitScope)scope);
        var breakLabel = loopScope.GetBreakLabel();
        var continueLabel = loopScope.GetContinueLabel();
        var auxLabel = loopScope.GetAuxLabel();

        return new GenericLoopStatement(
            loopScope,
            new GoToStatement(continueLabel),
            _testExpression,
            null,
            _body,
            breakLabel,
            auxLabel,
            continueLabel,
            null
        ).Lower(loopScope);
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");
}

using System.Diagnostics;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ForStatement : IBlockItem
{
    private readonly IExpression? _initExpression;
    private readonly IExpression? _testExpression;
    private readonly IExpression? _updateExpression;
    private readonly IBlockItem _body;

    public ForStatement(Ast.ForStatement statement)
    {
        var (initExpression, testExpression, updateExpression, body) = statement;
        _initExpression = initExpression?.ToIntermediate();
        _testExpression = testExpression?.ToIntermediate();
        _updateExpression = updateExpression?.ToIntermediate();
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
            new ExpressionStatement(_initExpression),
            _testExpression,
            _updateExpression,
            _body,
            breakLabel,
            auxLabel,
            null,
            continueLabel
        ).Lower(loopScope);
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");
}

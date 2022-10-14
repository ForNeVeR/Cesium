using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class WhileStatement : IBlockItem
{
    private readonly IExpression _testExpression;
    private readonly IBlockItem _body;
    private readonly string? _breakLabel;
    private readonly string? _continueLabel;

    public WhileStatement(Ast.WhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private WhileStatement(
        IExpression testExpression,
        IBlockItem body,
        string breakLabel,
        string continueLabel)
    {
        _testExpression = testExpression;
        _body = body;
        _breakLabel = breakLabel;
        _continueLabel = continueLabel;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var loopScope = new LoopScope((IEmitScope)scope);
        var breakLabel = loopScope.GetBreakLabel();
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        scope.AddLabel(breakLabel);
        var continueLabel = loopScope.GetContinueLabel();
        scope.AddLabel(continueLabel);
        return new WhileStatement(
            _testExpression.Lower(loopScope),
            _body.Lower(loopScope),
            breakLabel,
            continueLabel);
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;

    public void EmitTo(IEmitScope scope)
    {
        Debug.Assert(_breakLabel != null);
        Debug.Assert(_continueLabel != null);
        var loopScope = scope;

        var bodyProcessor = loopScope.Method.Body.GetILProcessor();
        var instructions = bodyProcessor.Body.Instructions;

        var loopIterationStart = scope.ResolveLabel(_continueLabel);
        bodyProcessor.Append(loopIterationStart);

        var testStartIndex = instructions.Count;
        _testExpression.EmitTo(loopScope);

        var exitLoop = scope.ResolveLabel(_breakLabel);
        bodyProcessor.Emit(OpCodes.Brfalse, exitLoop);

        _body.EmitTo(loopScope);

        var testStart = instructions[testStartIndex];
        var brToTest = bodyProcessor.Create(OpCodes.Br, testStart);
        bodyProcessor.Append(brToTest);

        bodyProcessor.Append(exitLoop);
    }
}

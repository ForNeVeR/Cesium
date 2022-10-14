using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DoWhileStatement : IBlockItem
{
    private readonly IExpression _testExpression;
    private readonly IBlockItem _body;
    private readonly string? _breakLabel;
    private readonly string? _continueLabel;

    public DoWhileStatement(Ast.DoWhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private DoWhileStatement(
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
        return new DoWhileStatement(
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

        var bodyStartIndex = instructions.Count;

        _body.EmitTo(loopScope);

        var loopIterationStart = scope.ResolveLabel(_continueLabel);
        bodyProcessor.Append(loopIterationStart);

        _testExpression.EmitTo(loopScope);
        var bodyStart = instructions[bodyStartIndex];
        var brToStart = bodyProcessor.Create(OpCodes.Brtrue, bodyStart);
        bodyProcessor.Append(brToStart);

        var exitLoop = scope.ResolveLabel(_breakLabel);
        bodyProcessor.Append(exitLoop);
    }
}

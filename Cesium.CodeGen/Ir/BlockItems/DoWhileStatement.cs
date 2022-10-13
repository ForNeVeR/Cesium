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

    public DoWhileStatement(Ast.DoWhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private DoWhileStatement(
        IExpression testExpression,
        IBlockItem body,
        string breakLabel)
    {
        _testExpression = testExpression;
        _body = body;
        _breakLabel = breakLabel;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var loopScope = new LoopScope((IEmitScope)scope);
        var breakLabel = loopScope.GetBreakLabel();
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        scope.AddLabel(breakLabel);
        return new DoWhileStatement(
            _testExpression.Lower(loopScope),
            _body.Lower(loopScope),
            breakLabel);
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;

    public void EmitTo(IEmitScope scope)
    {
        Debug.Assert(_breakLabel != null);
        var forScope = scope;

        var bodyProcessor = forScope.Method.Body.GetILProcessor();
        var instructions = bodyProcessor.Body.Instructions;

        var bodyStartIndex = instructions.Count;

        _body.EmitTo(forScope);

        _testExpression.EmitTo(forScope);
        var bodyStart = instructions[bodyStartIndex];
        var brToStart = bodyProcessor.Create(OpCodes.Brtrue, bodyStart);
        bodyProcessor.Append(brToStart);

        var exitLoop = scope.ResolveLabel(_breakLabel);
        bodyProcessor.Append(exitLoop);
    }
}

using System.Diagnostics;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ForStatement : IBlockItem
{
    private readonly IExpression? _initExpression;
    private readonly IExpression _testExpression;
    private readonly IExpression? _updateExpression;
    private readonly IBlockItem _body;
    private readonly string? _breakLabel;
    private readonly string? _continueLabel;

    public ForStatement(Ast.ForStatement statement)
    {
        var (initExpression, testExpression, updateExpression, body) = statement;
        _initExpression = initExpression?.ToIntermediate();
        // 6.8.5.3.2 if testExpression is null it should be replaced by nonzero constant
        _testExpression = testExpression?.ToIntermediate() ?? new ConstantLiteralExpression(new IntegerConstant("1"));
        _updateExpression = updateExpression?.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private ForStatement(
        IExpression? initExpression,
        IExpression testExpression,
        IExpression? updateExpression,
        IBlockItem body,
        string breakLabel,
        string continueLabel)
    {
        _initExpression = initExpression;
        _testExpression = testExpression;
        _updateExpression = updateExpression;
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
        return new ForStatement(
            _initExpression?.Lower(loopScope),
            _testExpression.Lower(loopScope),
            _updateExpression?.Lower(loopScope),
            _body.Lower(loopScope),
            breakLabel,
            continueLabel);
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;

    public void EmitTo(IEmitScope scope)
    {
        Debug.Assert(_breakLabel != null);
        Debug.Assert(_continueLabel != null);
        var forScope = scope;

        var bodyProcessor = forScope.Method.Body.GetILProcessor();
        var instructions = bodyProcessor.Body.Instructions;
        var stub = bodyProcessor.Create(OpCodes.Nop);

        _initExpression?.EmitTo(forScope);
        var brToTest = bodyProcessor.Create(OpCodes.Br, stub);
        bodyProcessor.Append(brToTest);
        var loopStartIndex = instructions.Count;
        _body.EmitTo(forScope);

        var loopIterationStart = scope.ResolveLabel(_continueLabel);
        bodyProcessor.Append(loopIterationStart);

        _updateExpression?.EmitTo(forScope);
        var testStartIndex = instructions.Count;
        _testExpression.EmitTo(forScope);
        var testStart = instructions[testStartIndex];
        brToTest.Operand = testStart;

        var loopStart = instructions[loopStartIndex];
        bodyProcessor.Emit(OpCodes.Brtrue, loopStart);
        bodyProcessor.Append(scope.ResolveLabel(_breakLabel));
    }
}

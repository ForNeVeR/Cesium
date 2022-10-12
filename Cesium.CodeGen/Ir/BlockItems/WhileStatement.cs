using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class WhileStatement : IBlockItem
{
    private readonly IExpression _testExpression;
    private readonly IBlockItem _body;
    private readonly string? _breakLabel;

    public WhileStatement(Ast.WhileStatement statement)
    {
        var (testExpression, body) = statement;

        _testExpression = testExpression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private WhileStatement(
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
        return new WhileStatement(
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

        var testStartIndex = instructions.Count;
        _testExpression.EmitTo(forScope);

        var exitLoop = scope.ResolveLabel(_breakLabel);
        bodyProcessor.Emit(OpCodes.Brfalse, exitLoop);

        _body.EmitTo(forScope);

        var testStart = instructions[testStartIndex];
        var brToTest = bodyProcessor.Create(OpCodes.Br, testStart);
        bodyProcessor.Append(brToTest);

        bodyProcessor.Append(exitLoop);
    }
}

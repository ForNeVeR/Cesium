using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Mono.Cecil.Cil;
using ConstantExpression = Cesium.CodeGen.Ir.Expressions.ConstantExpression;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ForStatement : IBlockItem
{
    private readonly IExpression? _initExpression;
    private readonly IExpression _testExpression;
    private readonly IExpression? _updateExpression;
    private readonly IBlockItem _body;

    public ForStatement(Ast.ForStatement statement)
    {
        var (initExpression, testExpression, updateExpression, body) = statement;
        _initExpression = initExpression?.ToIntermediate();
        // 6.8.5.3.2 if testExpression is null it should be replaced by nonzero constant
        _testExpression = testExpression?.ToIntermediate() ?? new ConstantExpression(new IntegerConstant("1"));
        _updateExpression = updateExpression?.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private ForStatement(
        IExpression? initExpression,
        IExpression testExpression,
        IExpression? updateExpression,
        IBlockItem body)
    {
        _initExpression = initExpression;
        _testExpression = testExpression;
        _updateExpression = updateExpression;
        _body = body;
    }

    public IBlockItem Lower()
        => new ForStatement(
            _initExpression?.Lower(),
            _testExpression.Lower(),
            _updateExpression?.Lower(),
            _body.Lower());

    public void EmitTo(IDeclarationScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var instructions = bodyProcessor.Body.Instructions;
        var stub = bodyProcessor.Create(OpCodes.Nop);

        _initExpression?.EmitTo(scope);
        var brToTest = bodyProcessor.Create(OpCodes.Br, stub);
        bodyProcessor.Append(brToTest);
        var loopStartIndex = instructions.Count;
        _body.EmitTo(scope);
        _updateExpression?.EmitTo(scope);
        var testStartIndex = instructions.Count;
        _testExpression.EmitTo(scope);
        var testStart = instructions[testStartIndex];
        brToTest.Operand = testStart;

        var loopStart = instructions[loopStartIndex];
        bodyProcessor.Emit(OpCodes.Brtrue, loopStart);
    }
}
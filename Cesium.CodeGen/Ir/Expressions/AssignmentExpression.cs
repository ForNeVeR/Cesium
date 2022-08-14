using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.Core.Exceptions;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class AssignmentExpression : BinaryOperatorExpression
{
    private readonly IValueExpression _target;

    internal AssignmentExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        _target = left as IValueExpression ?? throw new AssertException($"Not a value expression: {left}.");
    }

    public AssignmentExpression(Ast.AssignmentExpression expression) : base(expression)
    {
        _target = Left as IValueExpression ?? throw new AssertException($"Not a value expression: {Left}.");
    }

    public override IExpression Lower()
    {
        var rightExpanded = Operator switch
        {
            BinaryOperator.Assign => Right,
            BinaryOperator.AddAndAssign => new ArithmeticBinaryOperatorExpression(Left, BinaryOperator.Add, Right),
            BinaryOperator.SubtractAndAssign => new ArithmeticBinaryOperatorExpression(Left, BinaryOperator.Subtract, Right),
            BinaryOperator.MultiplyAndAssign => new ArithmeticBinaryOperatorExpression(Left, BinaryOperator.Multiply, Right),
            BinaryOperator.BitwiseLeftShiftAndAssign => new BitwiseBinaryOperatorExpression(Left, BinaryOperator.BitwiseLeftShift, Right),
            BinaryOperator.BitwiseRightShiftAndAssign => new BitwiseBinaryOperatorExpression(Left, BinaryOperator.BitwiseRightShift, Right),
            BinaryOperator.BitwiseOrAndAssign => new BitwiseBinaryOperatorExpression(Left, BinaryOperator.BitwiseOr, Right),
            BinaryOperator.BitwiseAndAndAssign => new BitwiseBinaryOperatorExpression(Left, BinaryOperator.BitwiseAnd, Right),
            BinaryOperator.BitwiseXorAndAssign => new BitwiseBinaryOperatorExpression(Left, BinaryOperator.BitwiseXor, Right),
            _ => throw new WipException(226, $"Assignment operator not supported, yet: {Operator}.")
        };

        return new AssignmentExpression(Left.Lower(), BinaryOperator.Assign, rightExpanded.Lower());
    }

    public override void EmitTo(IDeclarationScope scope)
    {
        if (Operator != BinaryOperator.Assign)
            throw new AssertException($"Operator {Operator} should've been lowered before emitting.");

        var value = _target.Resolve(scope);
        if (value is not ILValue lvalue)
            throw new CompilationException($"Not an lvalue: {value}.");

        lvalue.EmitSetValue(scope, Right);
    }

    // `x = v` expression returns type of x (and v)
    // e.g `int x; int y; x = (y = 10);`
    public override TypeReference GetExpressionType(IDeclarationScope scope) => _target.Resolve(scope).GetValueType();
}

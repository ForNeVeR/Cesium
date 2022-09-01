using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

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

    public override IExpression Lower(IDeclarationScope scope)
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

        IExpression left = Left.Lower(scope);
        IExpression right = rightExpanded.Lower(scope);
        IType leftType = left.GetExpressionType(scope);
        IType rightType = right.GetExpressionType(scope);
        if (scope.CTypeSystem.IsConversionAvailable(rightType, leftType)
            && !rightType.Equals(leftType))
        {
            right = new TypeCastExpression(leftType, right);
        }

        return new AssignmentExpression(left, BinaryOperator.Assign, right);
    }

    public override void EmitTo(IEmitScope scope)
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
    public override IType GetExpressionType(IDeclarationScope scope) => _target.Resolve(scope).GetValueType();
}

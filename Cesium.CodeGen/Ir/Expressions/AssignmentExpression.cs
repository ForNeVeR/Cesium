using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions;

internal class AssignmentExpression : BinaryOperatorExpression
{
    private readonly ILValueExpression _target;

    internal AssignmentExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        _target = left as ILValueExpression ?? throw new NotSupportedException($"Not an lvalue: {left}.");
    }

    public AssignmentExpression(Ast.AssignmentExpression expression) : base(expression)
    {
        _target = Left as ILValueExpression ?? throw new NotSupportedException($"Not an lvalue: {Left}.");
    }

    public override IExpression Lower() => Operator switch
    {
        BinaryOperator.Assign => new AssignmentExpression(Left.Lower(), BinaryOperator.Assign, Right.Lower()),
        BinaryOperator.AddAndAssign => LowerSmthAndAssign(BinaryOperator.Add),
        BinaryOperator.MultiplyAndAssign => LowerSmthAndAssign(BinaryOperator.Multiply),
        BinaryOperator.BitwiseLeftShiftAndAssign => LowerSmthAndAssign(BinaryOperator.BitwiseLeftShift),
        BinaryOperator.BitwiseRightShiftAndAssign => LowerSmthAndAssign(BinaryOperator.BitwiseRightShift),
        BinaryOperator.BitwiseOrAndAssign => LowerSmthAndAssign(BinaryOperator.BitwiseOr),
        BinaryOperator.BitwiseAndAndAssign => LowerSmthAndAssign(BinaryOperator.BitwiseAnd),
        BinaryOperator.BitwiseXorAndAssign => LowerSmthAndAssign(BinaryOperator.BitwiseXor),
        _ => throw new NotImplementedException($"Assignment operator not supported, yet: {Operator}.")
    };

    private AssignmentExpression LowerSmthAndAssign(BinaryOperator @operator)
        => new(Left, BinaryOperator.Assign, new BinaryOperatorExpression(Left, @operator, Right.Lower()));

    public override void EmitTo(IDeclarationScope scope)
    {
        if (Operator != BinaryOperator.Assign)
            throw new NotSupportedException($"Operator {Operator} should've been lowered before emitting.");

        Right.EmitTo(scope);

        _target.Resolve(scope).EmitSetValue(scope);
    }
}

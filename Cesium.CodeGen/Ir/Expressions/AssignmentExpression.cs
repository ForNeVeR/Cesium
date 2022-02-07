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
        BinaryOperator.AddAndAssign => new AssignmentExpression(
            Left,
            BinaryOperator.Assign,
            new BinaryOperatorExpression(Left, BinaryOperator.Add, Right.Lower())),
        BinaryOperator.MultiplyAndAssign => new AssignmentExpression(
            Left,
            BinaryOperator.Assign,
            new BinaryOperatorExpression(Left, BinaryOperator.Multiply, Right.Lower())),
        _ => throw new NotImplementedException($"Assignment operator not supported, yet: {Operator}.")
    };

    public override void EmitTo(FunctionScope scope)
    {
        if (Operator != BinaryOperator.Assign)
            throw new NotSupportedException($"Operator {Operator} should've been lowered before emitting.");

        Right.EmitTo(scope);

        _target.Resolve(scope).EmitSetValue(scope);
    }
}

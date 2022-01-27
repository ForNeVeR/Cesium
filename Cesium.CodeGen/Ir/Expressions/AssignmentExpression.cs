using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.Expressions;

internal class AssignmentExpression : BinaryOperatorExpression
{
    protected AssignmentExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
    }

    public AssignmentExpression(Ast.AssignmentExpression expression) : base(expression)
    {
    }

    public override IExpression Lower() => Operator switch
    {
        BinaryOperator.Assign => new AssignmentExpression(Left.Lower(), BinaryOperator.Assign, Right.Lower()),
        BinaryOperator.AddAndAssign => new AssignmentExpression(
            Left,
            BinaryOperator.Assign,
            new BinaryOperatorExpression(Left, BinaryOperator.Add, Right)),
        _ => throw new NotImplementedException($"Assignment operator not supported, yet: {Operator}.")
    };

    public override void EmitTo(FunctionScope scope)
    {
        if (Operator != BinaryOperator.Assign)
            throw new NotSupportedException($"Operator {Operator} should've been lowered before emitting.");

        Right.EmitTo(scope);

        var variableName = ((AstExpression)Left).ConstantIdentifier;
        scope.StLoc(scope.Variables[variableName]);
    }
}

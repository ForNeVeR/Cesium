using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class BinaryOperatorExpression : IExpression
{
    protected readonly IExpression Left;
    protected readonly BinaryOperator Operator;
    protected readonly IExpression Right;

    internal BinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public BinaryOperatorExpression(Ast.BinaryOperatorExpression expression)
    {
        var (left, @operator, right) = expression;
        Left = left.ToIntermediate();
        Operator = GetOperatorKind(expression.Operator);
        Right = right.ToIntermediate();
    }

    public virtual IExpression Lower() => new BinaryOperatorExpression(Left.Lower(), Operator, Right.Lower());

    public virtual void EmitTo(FunctionScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);
        scope.Method.Body.Instructions.Add(GetInstruction());

        Instruction GetInstruction() => Operator switch
        {
            BinaryOperator.Add => Instruction.Create(OpCodes.Add),
            BinaryOperator.Multiply => Instruction.Create(OpCodes.Mul),
            _ => throw new NotSupportedException($"Unsupported binary operator: {Operator}.")
        };
    }

    private static BinaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "+" => BinaryOperator.Add,
        "*" => BinaryOperator.Multiply,
        "=" => BinaryOperator.Assign,
        "+=" => BinaryOperator.AddAndAssign,
        "*=" => BinaryOperator.MultiplyAndAssign,
        _ => throw new NotImplementedException($"Binary operator not supported, yet: {@operator}.")
    };
}

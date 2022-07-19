using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Mono.Cecil;
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
        Operator = GetOperatorKind(@operator);
        Right = right.ToIntermediate();
    }

    public virtual IExpression Lower() => Operator switch
    {
        BinaryOperator.GreaterThanOrEqualTo => new BinaryOperatorExpression(
            new BinaryOperatorExpression(Left.Lower(), BinaryOperator.LessThan, Right.Lower()),
            BinaryOperator.EqualTo,
            new ConstantExpression(new IntegerConstant("0"))),
        BinaryOperator.LessThanOrEqualTo => new BinaryOperatorExpression(
            new BinaryOperatorExpression(Left.Lower(), BinaryOperator.GreaterThan, Right.Lower()),
            BinaryOperator.EqualTo,
            new ConstantExpression(new IntegerConstant("0"))),
        BinaryOperator.NotEqualTo => new BinaryOperatorExpression(
            new BinaryOperatorExpression(Left.Lower(), BinaryOperator.EqualTo, Right.Lower()),
            BinaryOperator.EqualTo,
            new ConstantExpression(new IntegerConstant("0"))),
        _ => new BinaryOperatorExpression(Left.Lower(), Operator, Right.Lower()),
    };

    public virtual void EmitTo(IDeclarationScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);
        scope.Method.Body.Instructions.Add(GetInstruction());

        Instruction GetInstruction() => Operator switch
        {
            BinaryOperator.Add => Instruction.Create(OpCodes.Add),
            BinaryOperator.Multiply => Instruction.Create(OpCodes.Mul),
            BinaryOperator.BitwiseLeftShift => Instruction.Create(OpCodes.Shl),
            BinaryOperator.BitwiseRightShift => Instruction.Create(OpCodes.Shr),
            BinaryOperator.BitwiseOr => Instruction.Create(OpCodes.Or),
            BinaryOperator.BitwiseAnd => Instruction.Create(OpCodes.And),
            BinaryOperator.BitwiseXor => Instruction.Create(OpCodes.Xor),
            BinaryOperator.GreaterThan => Instruction.Create(OpCodes.Cgt),
            BinaryOperator.LessThan => Instruction.Create(OpCodes.Clt),
            BinaryOperator.EqualTo => Instruction.Create(OpCodes.Ceq),
            _ => throw new NotSupportedException($"Unsupported binary operator: {Operator}.")
        };
    }

    // TODO[139]: Implement conversions and types tracking for arithmetic operations
    public virtual TypeReference GetExpressionType(IDeclarationScope scope) => throw new NotImplementedException();

    private static BinaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "+" => BinaryOperator.Add,
        "*" => BinaryOperator.Multiply,
        "=" => BinaryOperator.Assign,
        "+=" => BinaryOperator.AddAndAssign,
        "*=" => BinaryOperator.MultiplyAndAssign,
        "<<" => BinaryOperator.BitwiseLeftShift,
        ">>" => BinaryOperator.BitwiseRightShift,
        "|" => BinaryOperator.BitwiseOr,
        "&" => BinaryOperator.BitwiseAnd,
        "^" => BinaryOperator.BitwiseXor,
        "<<=" => BinaryOperator.BitwiseLeftShiftAndAssign,
        ">>=" => BinaryOperator.BitwiseRightShiftAndAssign,
        "|=" => BinaryOperator.BitwiseOrAndAssign,
        "&=" => BinaryOperator.BitwiseAndAndAssign,
        "^=" => BinaryOperator.BitwiseXorAndAssign,
        ">" => BinaryOperator.GreaterThan,
        "<" => BinaryOperator.LessThan,
        ">=" => BinaryOperator.GreaterThanOrEqualTo,
        "<=" => BinaryOperator.LessThanOrEqualTo,
        "==" => BinaryOperator.EqualTo,
        "!=" => BinaryOperator.NotEqualTo,
        "&&" => BinaryOperator.LogicalAnd,
        "||" => BinaryOperator.LogicalOr,
        _ => throw new NotImplementedException($"Binary operator not supported, yet: {@operator}.")
    };
}

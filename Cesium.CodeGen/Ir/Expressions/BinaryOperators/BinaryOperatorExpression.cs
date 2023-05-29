using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal abstract class BinaryOperatorExpression : IExpression
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

    protected BinaryOperatorExpression(Ast.BinaryOperatorExpression expression)
    {
        var (left, @operator, right) = expression;
        Left = left.ToIntermediate();
        Operator = GetOperatorKind(@operator);
        Right = right.ToIntermediate();
    }

    public abstract IExpression Lower(IDeclarationScope scope);
    public abstract IType GetExpressionType(IDeclarationScope scope);

    public void EmitTo(IEmitScope scope)
    {
        if (Operator is BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr)
            EmitShortCircuitingLogical(scope);
        else
            EmitPlain(scope);
    }

    private void EmitShortCircuitingLogical(IEmitScope scope)
    {
        void EmitLogicalAnd(IEmitScope scope)
        {
            var bodyProcessor = scope.Method.Body.GetILProcessor();
            var fastExitLabel = bodyProcessor.Create(OpCodes.Ldc_I4_0);

            Left.EmitTo(scope);
            bodyProcessor.Emit(OpCodes.Ldc_I4_0);
            bodyProcessor.Emit(OpCodes.Beq, fastExitLabel);

            Right.EmitTo(scope);
            bodyProcessor.Emit(OpCodes.Ldc_I4_1);
            bodyProcessor.Emit(OpCodes.Ceq);

            var exitLabel = bodyProcessor.Create(OpCodes.Nop);
            bodyProcessor.Emit(OpCodes.Br, exitLabel);

            bodyProcessor.Append(fastExitLabel);
            bodyProcessor.Append(exitLabel);
        }

        void EmitLogicalOr(IEmitScope scope)
        {
            var bodyProcessor = scope.Method.Body.GetILProcessor();
            var fastExitLabel = bodyProcessor.Create(OpCodes.Ldc_I4_1);

            Left.EmitTo(scope);
            bodyProcessor.Emit(OpCodes.Ldc_I4_1);
            bodyProcessor.Emit(OpCodes.Beq, fastExitLabel);

            Right.EmitTo(scope);
            bodyProcessor.Emit(OpCodes.Ldc_I4_1);
            bodyProcessor.Emit(OpCodes.Ceq);

            var exitLabel = bodyProcessor.Create(OpCodes.Nop);
            bodyProcessor.Emit(OpCodes.Br, exitLabel);

            bodyProcessor.Append(fastExitLabel);
            bodyProcessor.Append(exitLabel);
        }

        if (Operator == BinaryOperator.LogicalOr)
            EmitLogicalOr(scope);
        else
            EmitLogicalAnd(scope);
    }

    private void EmitPlain(IEmitScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);

        var opcode = Operator switch
        {
            // arithmetic operators
            BinaryOperator.Add => OpCodes.Add,
            BinaryOperator.Subtract => OpCodes.Sub,
            BinaryOperator.Multiply => OpCodes.Mul,
            BinaryOperator.Divide => OpCodes.Div,
            BinaryOperator.Remainder => OpCodes.Rem,

            // bitwise operators
            BinaryOperator.BitwiseAnd => OpCodes.And,
            BinaryOperator.BitwiseOr => OpCodes.Or,
            BinaryOperator.BitwiseXor => OpCodes.Xor,
            BinaryOperator.BitwiseLeftShift => OpCodes.Shl,
            BinaryOperator.BitwiseRightShift => OpCodes.Shr,

            // comparison operators
            BinaryOperator.GreaterThan => OpCodes.Cgt,
            BinaryOperator.LessThan => OpCodes.Clt,
            BinaryOperator.EqualTo => OpCodes.Ceq,

            _ => throw new AssertException($"Unsupported or non-lowered binary operator: {Operator}.")
        };

        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    private static BinaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "+" => BinaryOperator.Add,
        "-" => BinaryOperator.Subtract,
        "*" => BinaryOperator.Multiply,
        "/" => BinaryOperator.Divide,
        "=" => BinaryOperator.Assign,
        "+=" => BinaryOperator.AddAndAssign,
        "-=" => BinaryOperator.SubtractAndAssign,
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
        "%" => BinaryOperator.Remainder,
        _ => throw new WipException(226, $"Binary operator not supported, yet: {@operator}.")
    };
}

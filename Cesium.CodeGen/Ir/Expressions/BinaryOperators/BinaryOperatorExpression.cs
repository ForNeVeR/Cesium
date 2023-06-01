using System.Diagnostics;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class BinaryOperatorExpression : IExpression
{
    public IExpression Left { get; }
    public BinaryOperator Operator { get; }
    public IExpression Right { get; }

    internal BinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    internal BinaryOperatorExpression(Ast.BinaryOperatorExpression expression)
    {
        var (left, @operator, right) = expression;
        Left = left.ToIntermediate();
        Operator = GetOperatorKind(@operator);
        Right = right.ToIntermediate();
    }


    public IExpression Lower(IDeclarationScope scope)
    {
        var left = Left.Lower(scope);
        var right = Right.Lower(scope);

        // there's a possibility to check operand types for all the operators
        if (Operator.IsLogical() || Operator.IsBitwise())
            return new BinaryOperatorExpression(left, Operator, right);

        var leftType = left.GetExpressionType(scope);
        var rightType = right.GetExpressionType(scope);

        if (Operator.IsComparison())
        {
            if ((!scope.CTypeSystem.IsNumeric(leftType) && leftType is not PointerType)
                || (!scope.CTypeSystem.IsNumeric(rightType) && rightType is not PointerType))
                throw new CompilationException($"Unable to compare {leftType} to {rightType}");

            return new BinaryOperatorExpression(left, Operator, right);
        }

        // rest of the operators are arithmetic

        if (leftType is PointerType || rightType is PointerType)
        {
            return LowerPointerArithmetics(scope, left, right, leftType, rightType);
        }

        var commonType = scope.CTypeSystem.GetCommonNumericType(leftType, rightType);
        if (!leftType.IsEqualTo(commonType))
        {
            Debug.Assert(scope.CTypeSystem.IsConversionAvailable(leftType, commonType));
            left = new TypeCastExpression(commonType, left).Lower(scope);
        }

        if (!rightType.IsEqualTo(commonType))
        {
            Debug.Assert(scope.CTypeSystem.IsConversionAvailable(rightType, commonType));
            right = new TypeCastExpression(commonType, right).Lower(scope);
        }

        return new BinaryOperatorExpression(left, Operator, right);
    }

    private IExpression LowerPointerArithmetics(IDeclarationScope scope, IExpression left, IExpression right, IType leftType, IType rightType)
    {
        if (leftType is PointerType leftPointerType)
        {
            if (rightType is PointerType rightPointerType)
            {
                if (Operator != BinaryOperator.Subtract)
                {
                    throw new CompilationException($"Operator {Operator} is not supported for pointer/pointer operands");
                }

                if (!leftPointerType.Base.IsEqualTo(rightPointerType.Base))
                    throw new CompilationException("Invalid pointer subtraction - pointers are referencing different base types");

                var baseSize = leftPointerType.Base.GetSizeInBytesExpression(scope.ArchitectureSet);

                return new BinaryOperatorExpression(
                    new BinaryOperatorExpression(left, Operator, right),
                    BinaryOperator.Divide,
                    baseSize
                );
            }

            if (Operator != BinaryOperator.Add)
            {
                throw new CompilationException($"Operator {Operator} is not supported for pointer/value operands");
            }

            right = new TypeCastExpression(
                scope.CTypeSystem.NativeInt,
                new BinaryOperatorExpression(
                    leftPointerType.Base.GetSizeInBytesExpression(scope.ArchitectureSet),
                    BinaryOperator.Multiply,
                    right));

            return new BinaryOperatorExpression(left, Operator, right);
        }
        else
        {
            var rightPointerType = (PointerType)rightType;

            if (Operator != BinaryOperator.Add)
            {
                throw new CompilationException($"Operator {Operator} is not supported for value/pointer operands");
            }

            left = new TypeCastExpression(
                scope.CTypeSystem.NativeInt,
                new BinaryOperatorExpression(
                    rightPointerType.Base.GetSizeInBytesExpression(scope.ArchitectureSet),
                    BinaryOperator.Multiply,
                    left));

            return new BinaryOperatorExpression(left, Operator, right);
        }
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        if (Operator.IsComparison() || Operator.IsLogical())
            return scope.CTypeSystem.Bool;

        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        if (Operator.IsArithmetic())
        {
            switch (leftType, rightType)
            {
                case (PointerType, not PointerType): return leftType;
                case (not PointerType, PointerType): return rightType;
                case (PointerType left, PointerType right):
                    Debug.Assert(left.Base.GetSizeInBytes(scope.ArchitectureSet) ==
                                 right.Base.GetSizeInBytes(scope.ArchitectureSet));

                    return scope.CTypeSystem.NativeInt; // ptrdiff_t, must be signed
            }
        }

        // both bitwise and arithmetic operators obey same arithmetic conversions
        // https://en.cppreference.com/w/c/language/operator_arithmetic
        return scope.CTypeSystem.GetCommonNumericType(leftType, rightType);
    }

    public void EmitTo(IEmitScope scope)
    {
        if (Operator is BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr)
            EmitShortCircuitingLogical(scope);
        else
            EmitPlain(scope);
    }

    private void EmitShortCircuitingLogical(IEmitScope scope)
    {
        var (shortCircuitResult, shortCircuitBranch) =
            Operator == BinaryOperator.LogicalOr
                ? (OpCodes.Ldc_I4_1, OpCodes.Brtrue)
                : (OpCodes.Ldc_I4_0, OpCodes.Brfalse);

        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var fastExitLabel = bodyProcessor.Create(shortCircuitResult);

        Left.EmitTo(scope);
        bodyProcessor.Emit(shortCircuitBranch, fastExitLabel);

        Right.EmitTo(scope);

        var exitLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Br, exitLabel);

        bodyProcessor.Append(fastExitLabel);
        bodyProcessor.Append(exitLabel);
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

            // comparison operators (negated)
            BinaryOperator.GreaterThanOrEqualTo => OpCodes.Clt,
            BinaryOperator.LessThanOrEqualTo => OpCodes.Cgt,
            BinaryOperator.NotEqualTo => OpCodes.Ceq,

            _ => throw new AssertException($"Unsupported or non-lowered binary operator: {Operator}.")
        };

        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));

        // negate result of that operators
        // a >= b <-> !(a < b)
        // a <= b <-> !(a > b)
        // a != b <-> !(a == b)
        if (Operator
            is BinaryOperator.GreaterThanOrEqualTo
            or BinaryOperator.LessThanOrEqualTo
            or BinaryOperator.NotEqualTo)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
        }
    }

    private static BinaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "+" => BinaryOperator.Add,
        "-" => BinaryOperator.Subtract,
        "*" => BinaryOperator.Multiply,
        "/" => BinaryOperator.Divide,
        "<<" => BinaryOperator.BitwiseLeftShift,
        ">>" => BinaryOperator.BitwiseRightShift,
        "|" => BinaryOperator.BitwiseOr,
        "&" => BinaryOperator.BitwiseAnd,
        "^" => BinaryOperator.BitwiseXor,
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

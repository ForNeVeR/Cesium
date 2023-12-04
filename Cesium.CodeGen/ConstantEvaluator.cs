using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;

namespace Cesium.CodeGen;

internal static class ConstantEvaluator
{
    public static IConstant GetConstantValue(IExpression expression)
    {
        switch (expression)
        {
            case ConstantLiteralExpression literal:
                return literal.Constant;

            case UnaryOperatorExpression unOp:
            {
                var constant = GetConstantValue(unOp.Target);

                if (constant is not IntegerConstant constInt)
                    throw new CompilationException("Evaluated constant is not an integer");

                    return unOp.Operator switch
                    {
                        UnaryOperator.Negation => new IntegerConstant(-constInt.Value),
                        UnaryOperator.BitwiseNot => new IntegerConstant(~constInt.Value),
                        UnaryOperator.LogicalNot => new IntegerConstant(constInt.Value != 0 ? 0 : 1),
                        UnaryOperator.AddressOf or UnaryOperator.Indirection => throw new CompilationException($"Operator {unOp.Operator} is not compile-time evaluable"),
                        _ => throw new ArgumentOutOfRangeException($"Invalid unary operator {unOp.Operator}"),
                    };
                }

            case BinaryOperatorExpression binOp:
            {
                var leftConstant = GetConstantValue(binOp.Left);
                var rightConstant = GetConstantValue(binOp.Right);

                if (leftConstant is not IntegerConstant leftInt ||
                    rightConstant is not IntegerConstant rightInt)
                    throw new CompilationException("Evaluated constants are not integer");

                return binOp.Operator switch
                    {
                        BinaryOperator.Add => new IntegerConstant(leftInt.Value + rightInt.Value),
                        BinaryOperator.Subtract => new IntegerConstant(leftInt.Value - rightInt.Value),
                        BinaryOperator.Multiply => new IntegerConstant(leftInt.Value * rightInt.Value),
                        BinaryOperator.Divide => new IntegerConstant(leftInt.Value / rightInt.Value),
                        BinaryOperator.Remainder => new IntegerConstant(leftInt.Value % rightInt.Value),
                        BinaryOperator.BitwiseLeftShift => new IntegerConstant(leftInt.Value << (int)rightInt.Value),
                        BinaryOperator.BitwiseRightShift => new IntegerConstant(leftInt.Value >> (int)rightInt.Value),
                        BinaryOperator.BitwiseOr => new IntegerConstant(leftInt.Value | rightInt.Value),
                        BinaryOperator.BitwiseAnd => new IntegerConstant(leftInt.Value & rightInt.Value),
                        BinaryOperator.BitwiseXor => new IntegerConstant(leftInt.Value ^ rightInt.Value),
                        // boolean constants are needed here
                        BinaryOperator.GreaterThan => new IntegerConstant(leftInt.Value > rightInt.Value ? 1 : 0),
                        BinaryOperator.LessThan => new IntegerConstant(leftInt.Value < rightInt.Value ? 1 : 0),
                        BinaryOperator.GreaterThanOrEqualTo => new IntegerConstant(leftInt.Value >= rightInt.Value ? 1 : 0),
                        BinaryOperator.LessThanOrEqualTo => new IntegerConstant(leftInt.Value <= rightInt.Value ? 1 : 0),
                        BinaryOperator.EqualTo => new IntegerConstant(leftInt.Value == rightInt.Value ? 1 : 0),
                        BinaryOperator.NotEqualTo => new IntegerConstant(leftInt.Value != rightInt.Value ? 1 : 0),
                        BinaryOperator.LogicalAnd => new IntegerConstant((leftInt.Value != 0) && (rightInt.Value != 0) ? 1 : 0),
                        BinaryOperator.LogicalOr => new IntegerConstant((leftInt.Value != 0) || (rightInt.Value != 0) ? 1 : 0),
                        _ => throw new ArgumentOutOfRangeException($"Invalid binary operator {binOp.Operator}"),
                    };
                }

            default:
                throw new CompilationException($"Expression {expression} cannot be evaluated as constant expression.");
        }
    }
}

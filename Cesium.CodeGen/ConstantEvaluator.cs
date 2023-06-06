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

                switch (unOp.Operator)
                {
                    case UnaryOperator.Negation:
                        return new IntegerConstant(-constInt.Value);
                    case UnaryOperator.BitwiseNot:
                        return new IntegerConstant(~constInt.Value);
                    case UnaryOperator.LogicalNot:
                        return new IntegerConstant(constInt.Value != 0 ? 0 : 1);
                    case UnaryOperator.AddressOf:
                    case UnaryOperator.Indirection:
                        throw new CompilationException($"Operator {unOp.Operator} is not compile-time evaluable");
                    default:
                        throw new ArgumentOutOfRangeException($"Invalid unary operator {unOp.Operator}");
                }
            }

            case BinaryOperatorExpression binOp:
            {
                var leftConstant = GetConstantValue(binOp.Left);
                var rightConstant = GetConstantValue(binOp.Right);

                if (leftConstant is not IntegerConstant leftInt ||
                    rightConstant is not IntegerConstant rightInt)
                    throw new CompilationException("Evaluated constants are not integer");

                switch (binOp.Operator)
                {
                    case BinaryOperator.Add:
                        return new IntegerConstant(leftInt.Value + rightInt.Value);
                    case BinaryOperator.Subtract:
                        return new IntegerConstant(leftInt.Value - rightInt.Value);
                    case BinaryOperator.Multiply:
                        return new IntegerConstant(leftInt.Value * rightInt.Value);
                    case BinaryOperator.Divide:
                        return new IntegerConstant(leftInt.Value / rightInt.Value);
                    case BinaryOperator.Remainder:
                        return new IntegerConstant(leftInt.Value % rightInt.Value);
                    case BinaryOperator.BitwiseLeftShift:
                        return new IntegerConstant(leftInt.Value << rightInt.Value);
                    case BinaryOperator.BitwiseRightShift:
                        return new IntegerConstant(leftInt.Value >> rightInt.Value);
                    case BinaryOperator.BitwiseOr:
                        return new IntegerConstant(leftInt.Value | rightInt.Value);
                    case BinaryOperator.BitwiseAnd:
                        return new IntegerConstant(leftInt.Value & rightInt.Value);
                    case BinaryOperator.BitwiseXor:
                        return new IntegerConstant(leftInt.Value ^ rightInt.Value);
                    // boolean constants are needed here
                    case BinaryOperator.GreaterThan:
                        return new IntegerConstant(leftInt.Value > rightInt.Value ? 1 : 0);
                    case BinaryOperator.LessThan:
                        return new IntegerConstant(leftInt.Value < rightInt.Value ? 1 : 0);
                    case BinaryOperator.GreaterThanOrEqualTo:
                        return new IntegerConstant(leftInt.Value >= rightInt.Value ? 1 : 0);
                    case BinaryOperator.LessThanOrEqualTo:
                        return new IntegerConstant(leftInt.Value <= rightInt.Value ? 1 : 0);
                    case BinaryOperator.EqualTo:
                        return new IntegerConstant(leftInt.Value == rightInt.Value ? 1 : 0);
                    case BinaryOperator.NotEqualTo:
                        return new IntegerConstant(leftInt.Value != rightInt.Value ? 1 : 0);
                    case BinaryOperator.LogicalAnd:
                        return new IntegerConstant((leftInt.Value != 0) && (rightInt.Value != 0) ? 1 : 0);
                    case BinaryOperator.LogicalOr:
                        return new IntegerConstant((leftInt.Value != 0) || (rightInt.Value != 0) ? 1 : 0);
                    default:
                        throw new ArgumentOutOfRangeException($"Invalid binary operator {binOp.Operator}");
                }
            }

            default:
                throw new CompilationException($"Expression {expression} cannot be evaluated as constant expression.");
        }
    }
}

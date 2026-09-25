// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;

namespace Cesium.CodeGen;

internal static class ConstantEvaluator
{
    public static IConstant GetConstantValue(IExpression expression, IDeclarationScope? scope)
    {
        var result = TryGetConstantValue(expression, scope);
        if (result.ErrorMessage is not null)
        {
            throw new CompilationException(result.ErrorMessage);
        }

        Debug.Assert(result.Constant != null);
        return result.Constant;
    }

    public static (string? ErrorMessage, IConstant? Constant) TryGetConstantValue(IExpression expression, IDeclarationScope? scope)
    {
        try
        {
            switch (expression)
            {
                case ConstantLiteralExpression literal:
                    return (null, literal.Constant);

                case UnaryOperatorExpression unOp:
                {
                    var constant = GetConstantValue(unOp.Target, scope);

                    if (constant is not IntegerConstant constInt)
                        return ("Evaluated constant is not an integer", null);

                    return unOp.Operator switch
                    {
                        UnaryOperator.Negation => (null, new IntegerConstant(-constInt.Value)),
                        UnaryOperator.BitwiseNot => (null, new IntegerConstant(~constInt.Value)),
                        UnaryOperator.LogicalNot => (null, new IntegerConstant(constInt.Value != 0 ? 0 : 1)),
                        UnaryOperator.AddressOf or UnaryOperator.Indirection => (
                            $"Operator {unOp.Operator} is not compile-time evaluable", null),
                        _ => throw new ArgumentOutOfRangeException($"Invalid unary operator {unOp.Operator}."),
                    };
                }

                case BinaryOperatorExpression binOp:
                {
                    var leftConstant = GetConstantValue(binOp.Left, scope);
                    var rightConstant = GetConstantValue(binOp.Right, scope);

                    if (leftConstant is not IntegerConstant leftInt ||
                        rightConstant is not IntegerConstant rightInt)
                        return ($"Evaluated constants ({leftConstant}, {rightConstant}) are not integer.", null);

                    return binOp.Operator switch
                    {
                        BinaryOperator.Add => (null, new IntegerConstant(leftInt.Value + rightInt.Value)),
                        BinaryOperator.Subtract => (null, new IntegerConstant(leftInt.Value - rightInt.Value)),
                        BinaryOperator.Multiply => (null, new IntegerConstant(leftInt.Value * rightInt.Value)),
                        BinaryOperator.Divide => (null, new IntegerConstant(leftInt.Value / rightInt.Value)),
                        BinaryOperator.Remainder => (null, new IntegerConstant(leftInt.Value % rightInt.Value)),
                        BinaryOperator.BitwiseLeftShift => (null,
                            new IntegerConstant(leftInt.Value << (int)rightInt.Value)),
                        BinaryOperator.BitwiseRightShift => (null,
                            new IntegerConstant(leftInt.Value >> (int)rightInt.Value)),
                        BinaryOperator.BitwiseOr => (null, new IntegerConstant(leftInt.Value | rightInt.Value)),
                        BinaryOperator.BitwiseAnd => (null, new IntegerConstant(leftInt.Value & rightInt.Value)),
                        BinaryOperator.BitwiseXor => (null, new IntegerConstant(leftInt.Value ^ rightInt.Value)),
                        // boolean constants are needed here
                        BinaryOperator.GreaterThan => (null,
                            new IntegerConstant(leftInt.Value > rightInt.Value ? 1 : 0)),
                        BinaryOperator.LessThan => (null, new IntegerConstant(leftInt.Value < rightInt.Value ? 1 : 0)),
                        BinaryOperator.GreaterThanOrEqualTo => (null,
                            new IntegerConstant(leftInt.Value >= rightInt.Value ? 1 : 0)),
                        BinaryOperator.LessThanOrEqualTo => (null,
                            new IntegerConstant(leftInt.Value <= rightInt.Value ? 1 : 0)),
                        BinaryOperator.EqualTo => (null, new IntegerConstant(leftInt.Value == rightInt.Value ? 1 : 0)),
                        BinaryOperator.NotEqualTo => (null,
                            new IntegerConstant(leftInt.Value != rightInt.Value ? 1 : 0)),
                        BinaryOperator.LogicalAnd => (null,
                            new IntegerConstant((leftInt.Value != 0) && (rightInt.Value != 0) ? 1 : 0)),
                        BinaryOperator.LogicalOr => (null,
                            new IntegerConstant((leftInt.Value != 0) || (rightInt.Value != 0) ? 1 : 0)),
                        _ => throw new ArgumentOutOfRangeException($"Invalid binary operator {binOp.Operator}"),
                    };
                }

                case IdentifierExpression identifierExpression:
                {
                    if (scope != null)
                    {
                        var existingVariable = scope.GetVariable(identifierExpression.Identifier);
                        var constantValue = existingVariable?.Constant;
                        if (constantValue is not null)
                        {
                            return TryGetConstantValue(constantValue, scope);
                        }
                    }

                    return ($"Expression {expression} cannot be evaluated as constant expression.", null);
                }

                default:
                    return ($"Expression {expression} cannot be evaluated as constant expression.", null);
            }
        }
        catch (Exception e)
        {
            return (e.Message, null);
        }
    }

    public static ConditionalValue EvaluateCondition(IExpression condition)
    {
        var (results, val) = TryGetConstantValue(condition, null);

        if (results != null || val == null)
            return ConditionalValue.Unknown;

        var isEvaluated = true;

        return val switch
            {
                IntegerConstant integer => integer.Value == 0,
                FloatingPointConstant floatingPoint => floatingPoint.Value == 0,
                CharConstant charVal => charVal.Value == 0,
                StringConstant => false,
                // TODO: Handle NULL literals
                _ => isEvaluated = false
            } ? ConditionalValue.ConstantlyFalse
            : isEvaluated ? ConditionalValue.ConstantlyTrue : ConditionalValue.Unknown;
    }
}

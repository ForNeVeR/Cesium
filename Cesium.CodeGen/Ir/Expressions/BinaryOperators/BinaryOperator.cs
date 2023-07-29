namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

public enum BinaryOperator
{
    Add, // +
    Subtract, // -
    Multiply, // *
    Divide, // /
    Remainder, // %

    BitwiseLeftShift, // <<
    BitwiseRightShift, // >>
    BitwiseOr, // |
    BitwiseAnd, // &
    BitwiseXor, // ^

    GreaterThan, // >
    LessThan, // <
    GreaterThanOrEqualTo, // >=
    LessThanOrEqualTo, // <=
    EqualTo, // ==
    NotEqualTo, // !=

    LogicalAnd, // &&
    LogicalOr, // ||
}

public static class BinaryOperatorExtensions
{
    public static bool IsArithmetic(this BinaryOperator op) =>
        op is BinaryOperator.Add
           or BinaryOperator.Subtract
           or BinaryOperator.Multiply
           or BinaryOperator.Divide
           or BinaryOperator.Remainder;

    public static bool IsComparison(this BinaryOperator op) =>
        op is BinaryOperator.GreaterThan
           or BinaryOperator.GreaterThanOrEqualTo
           or BinaryOperator.LessThan
           or BinaryOperator.LessThanOrEqualTo
           or BinaryOperator.EqualTo
           or BinaryOperator.NotEqualTo;

    public static bool IsLogical(this BinaryOperator op) =>
        op is BinaryOperator.LogicalAnd
           or BinaryOperator.LogicalOr;

    public static bool IsBitwise(this BinaryOperator op) =>
        op is BinaryOperator.BitwiseAnd
           or BinaryOperator.BitwiseOr
           or BinaryOperator.BitwiseXor
           or BinaryOperator.BitwiseLeftShift
           or BinaryOperator.BitwiseRightShift;
}

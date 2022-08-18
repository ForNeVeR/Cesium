using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
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

    public abstract IExpression Lower();
    public abstract IType GetExpressionType(IDeclarationScope scope);
    public abstract void EmitTo(IDeclarationScope scope);

    protected void EmitConversion(IDeclarationScope scope, IType exprType, IType desiredType)
    {
        if (exprType.IsEqualTo(desiredType)
            || (scope.CTypeSystem.IsBool(exprType) && scope.CTypeSystem.IsInteger(desiredType))
            || (scope.CTypeSystem.IsBool(desiredType) && scope.CTypeSystem.IsInteger(exprType)))
            return;

        var ts = scope.CTypeSystem;
        if(!ts.IsNumeric(exprType))
            throw new CompilationException($"Conversion from {exprType} to {desiredType} is not supported.");

        if(desiredType.Equals(ts.SignedChar))
            Add(OpCodes.Conv_I1);
        else if(desiredType.Equals(ts.Short))
            Add(OpCodes.Conv_I2);
        else if(desiredType.Equals(ts.Int))
            Add(OpCodes.Conv_I4);
        else if(desiredType.Equals(ts.Long))
            Add(OpCodes.Conv_I8);
        else if(desiredType.Equals(ts.Char))
            Add(OpCodes.Conv_U1);
        else if(desiredType.Equals(ts.UnsignedShort))
            Add(OpCodes.Conv_U2);
        else if(desiredType.Equals(ts.UnsignedInt))
            Add(OpCodes.Conv_U4);
        else if(desiredType.Equals(ts.UnsignedLong))
            Add(OpCodes.Conv_U8);
        else if(desiredType.Equals(ts.Float))
            Add(OpCodes.Conv_R4);
        else if (desiredType.Equals(ts.Double))
            Add(OpCodes.Conv_R8);
        else
            throw new CompilationException($"Conversion from {exprType} to {desiredType} is not supported.");

        void Add(OpCode op) => scope.Method.Body.Instructions.Add(Instruction.Create(op));
    }

    private static BinaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "+" => BinaryOperator.Add,
        "-" => BinaryOperator.Subtract,
        "*" => BinaryOperator.Multiply,
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
        _ => throw new WipException(226, $"Binary operator not supported, yet: {@operator}.")
    };
}

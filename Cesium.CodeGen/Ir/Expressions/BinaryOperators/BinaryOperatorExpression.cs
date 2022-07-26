using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
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
    public abstract TypeReference GetExpressionType(IDeclarationScope scope);
    public abstract void EmitTo(IDeclarationScope scope);

    protected void EmitConversion(IDeclarationScope scope, TypeReference exprType, TypeReference desiredType)
    {
        if (exprType.IsEqualTo(desiredType))
            return;

        var ts = scope.TypeSystem;

        if(exprType.Equals(ts.SByte))
            Add(OpCodes.Conv_I1);
        else if(exprType.Equals(ts.Int16))
            Add(OpCodes.Conv_I2);
        else if(exprType.Equals(ts.Int32))
            Add(OpCodes.Conv_I4);
        else if(exprType.Equals(ts.Int64))
            Add(OpCodes.Conv_I8);
        else if(exprType.Equals(ts.Byte))
            Add(OpCodes.Conv_U1);
        else if(exprType.Equals(ts.UInt16))
            Add(OpCodes.Conv_U2);
        else if(exprType.Equals(ts.UInt32))
            Add(OpCodes.Conv_U4);
        else if(exprType.Equals(ts.UInt64))
            Add(OpCodes.Conv_U8);
        else if(exprType.Equals(ts.Single))
            Add(OpCodes.Conv_R4);
        else if (exprType.Equals(ts.Double))
            Add(OpCodes.Conv_R8);
        else
            throw new NotSupportedException($"Conversion from {exprType.Name} to {desiredType.Name} is not supported.");

        void Add(OpCode op) => scope.Method.Body.Instructions.Add(Instruction.Create(op));
    }

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

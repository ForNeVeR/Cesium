using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class ArithmeticBinaryOperatorExpression: BinaryOperatorExpression
{
    internal ArithmeticBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if (!Operator.IsArithmetic())
            throw new AssertException($"Operator {Operator} is not arithmetic.");
    }

    public ArithmeticBinaryOperatorExpression(Ast.ArithmeticBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower(IDeclarationScope scope)
    {
        var left = Left.Lower(scope);
        var right = Right.Lower(scope);
        var leftType = left.GetExpressionType(scope);
        var rightType = right.GetExpressionType(scope);

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

        return new ArithmeticBinaryOperatorExpression(left, Operator, right);
    }

    public override void EmitTo(IDeclarationScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);

        var opcode = Operator switch
        {
            BinaryOperator.Add => OpCodes.Add,
            BinaryOperator.Subtract => OpCodes.Sub,
            BinaryOperator.Multiply => OpCodes.Mul,
            _ => throw new AssertException($"Operator {Operator} is not arithmetic.")
        };
        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        return scope.CTypeSystem.GetCommonNumericType(leftType, rightType);
    }
}

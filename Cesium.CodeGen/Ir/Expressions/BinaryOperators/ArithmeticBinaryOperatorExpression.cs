using System.Diagnostics;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

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
        ValidateTypeOperations(leftType, rightType);
        if (leftType is PointerType || rightType is PointerType)
        {
            if (leftType is PointerType leftPointerType)
            {
                right = new TypeCastExpression(
                    scope.CTypeSystem.NativeInt,
                    new ArithmeticBinaryOperatorExpression(
                        new ConstantExpression(new IntegerConstant(leftPointerType.Base.SizeInBytes)),
                        BinaryOperator.Multiply,
                        right));

                return new ArithmeticBinaryOperatorExpression(left, Operator, right);
            }

            if (rightType is PointerType rightPointerType)
            {
                left = new TypeCastExpression(
                    scope.CTypeSystem.NativeInt,
                    new ArithmeticBinaryOperatorExpression(
                        new ConstantExpression(new IntegerConstant(rightPointerType.Base.SizeInBytes)),
                        BinaryOperator.Multiply,
                        left));

                return new ArithmeticBinaryOperatorExpression(left, Operator, right);
            }
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

        return new ArithmeticBinaryOperatorExpression(left, Operator, right);
    }

    public override void EmitTo(IEmitScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);

        var opcode = Operator switch
        {
            BinaryOperator.Add => OpCodes.Add,
            BinaryOperator.Subtract => OpCodes.Sub,
            BinaryOperator.Multiply => OpCodes.Mul,
            BinaryOperator.Remainder => OpCodes.Rem,
            _ => throw new AssertException($"Operator {Operator} is not arithmetic.")
        };
        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        if (leftType is PointerType || rightType is PointerType)
        {
            if (leftType is PointerType)
            {
                return leftType;
            }

            return rightType;
        }

        return scope.CTypeSystem.GetCommonNumericType(leftType, rightType);
    }

    private void ValidateTypeOperations(IType leftType, IType rightType)
    {
        if (leftType is PointerType || rightType is PointerType)
        {
            if (Operator == BinaryOperator.Multiply)
            {
                throw new CompilationException("Operator '*' does not suported on pointer types");
            }

            if (leftType is PointerType && rightType is PointerType)
            {
                if (Operator == BinaryOperator.Add)
                {
                    throw new CompilationException("Operator '+': cannot add two pointers");
                }

                if (Operator == BinaryOperator.Subtract)
                {
                    throw new WipException(260, "Pointer subtraction not implemented.");

                }
            }
        }
    }
}

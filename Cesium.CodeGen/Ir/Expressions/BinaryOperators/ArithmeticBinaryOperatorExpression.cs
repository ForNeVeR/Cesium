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

        return new ArithmeticBinaryOperatorExpression(left, Operator, right);
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

                var baseSizeLeft = leftPointerType.Base.GetSizeInBytes(scope.ArchitectureSet);
                var baseSizeRight = rightPointerType.Base.GetSizeInBytes(scope.ArchitectureSet);

                if (baseSizeLeft == null || baseSizeRight == null || baseSizeLeft != baseSizeRight)
                    throw new CompilationException("Invalid pointer subtraction - pointers are referencing different base types");

                return new ArithmeticBinaryOperatorExpression(
                    new ArithmeticBinaryOperatorExpression(left, Operator, right),
                    BinaryOperator.Divide,
                    new ConstantLiteralExpression(new IntegerConstant(baseSizeLeft.Value))
                );
            }

            if (Operator != BinaryOperator.Add)
            {
                throw new CompilationException($"Operator {Operator} is not supported for pointer/value operands");
            }

            right = new TypeCastExpression(
                scope.CTypeSystem.NativeInt,
                new ArithmeticBinaryOperatorExpression(
                    leftPointerType.Base.GetSizeInBytesExpression(scope.ArchitectureSet),
                    BinaryOperator.Multiply,
                    right));

            return new ArithmeticBinaryOperatorExpression(left, Operator, right);
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
                new ArithmeticBinaryOperatorExpression(
                    rightPointerType.Base.GetSizeInBytesExpression(scope.ArchitectureSet),
                    BinaryOperator.Multiply,
                    left));

            return new ArithmeticBinaryOperatorExpression(left, Operator, right);
        }
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
            BinaryOperator.Divide => OpCodes.Div,
            BinaryOperator.Remainder => OpCodes.Rem,
            _ => throw new AssertException($"Operator {Operator} is not arithmetic.")
        };
        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        switch (leftType, rightType)
        {
            case (PointerType, not PointerType): return leftType;
            case (not PointerType, PointerType): return rightType;
            case (PointerType left, PointerType right):
                Debug.Assert(left.Base.GetSizeInBytes(scope.ArchitectureSet) == right.Base.GetSizeInBytes(scope.ArchitectureSet));

                return scope.CTypeSystem.Int; // ptrdiff_t, must be signed, int is "fine" (nint is not numeric anyway)
        }

        return scope.CTypeSystem.GetCommonNumericType(leftType, rightType);
    }
}

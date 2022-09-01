using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class ComparisonBinaryOperatorExpression: BinaryOperatorExpression
{
    internal ComparisonBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if (!Operator.IsComparison())
            throw new AssertException($"Operator {Operator} is not comparison.");
    }

    public ComparisonBinaryOperatorExpression(Ast.ComparisonBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower(IDeclarationScope scope)
    {
        var left = Left.Lower(scope);
        var right = Right.Lower(scope);
        var leftType = left.GetExpressionType(scope);
        var rightType = right.GetExpressionType(scope);
        if ((!scope.CTypeSystem.IsNumeric(leftType) && leftType is not PointerType)
            || (!scope.CTypeSystem.IsNumeric(rightType) && rightType is not PointerType))
            throw new CompilationException($"Unable to compare {leftType} to {rightType}");

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

        return Operator switch
        {
            BinaryOperator.GreaterThanOrEqualTo => new ComparisonBinaryOperatorExpression(
                new ComparisonBinaryOperatorExpression(left, BinaryOperator.LessThan, right),
                BinaryOperator.EqualTo,
                new ConstantExpression(new IntegerConstant("0"))
            ),
            BinaryOperator.LessThanOrEqualTo => new ComparisonBinaryOperatorExpression(
                new ComparisonBinaryOperatorExpression(left, BinaryOperator.GreaterThan, right),
                BinaryOperator.EqualTo,
                new ConstantExpression(new IntegerConstant("0"))
            ),
            BinaryOperator.NotEqualTo => new ComparisonBinaryOperatorExpression(
                new ComparisonBinaryOperatorExpression(left, BinaryOperator.EqualTo, right),
                BinaryOperator.EqualTo,
                new ConstantExpression(new IntegerConstant("0"))
            ),
            _ => new ComparisonBinaryOperatorExpression(left, Operator, right),
        };
    }

    public override void EmitTo(IEmitScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);
        scope.Method.Body.Instructions.Add(GetInstruction());

        Instruction GetInstruction() => Operator switch
        {
            BinaryOperator.GreaterThan => Instruction.Create(OpCodes.Cgt),
            BinaryOperator.LessThan => Instruction.Create(OpCodes.Clt),
            BinaryOperator.EqualTo => Instruction.Create(OpCodes.Ceq),
            _ => throw new AssertException($"Unsupported binary operator: {Operator}.")
        };
    }

    public override IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.Bool;
}

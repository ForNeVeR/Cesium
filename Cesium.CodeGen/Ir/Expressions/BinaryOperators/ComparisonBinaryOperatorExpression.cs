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

        return new ComparisonBinaryOperatorExpression(left, Operator, right);
    }
}

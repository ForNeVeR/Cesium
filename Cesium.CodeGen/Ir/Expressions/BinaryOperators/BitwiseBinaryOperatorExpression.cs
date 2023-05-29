using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class BitwiseBinaryOperatorExpression: BinaryOperatorExpression
{
    internal BitwiseBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if (!Operator.IsBitwise())
            throw new AssertException($"Operator {Operator} is not bitwise.");
    }

    public BitwiseBinaryOperatorExpression(Ast.BitwiseBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower(IDeclarationScope scope) => new BitwiseBinaryOperatorExpression(Left.Lower(scope), Operator, Right.Lower(scope));

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        if (!scope.CTypeSystem.IsInteger(leftType))
            throw new CompilationException($"Left operand of '{Operator}' is not of integer type: {Left}");

        var rightType = Right.GetExpressionType(scope);
        if (!scope.CTypeSystem.IsInteger(rightType))
            throw new CompilationException($"Right operand of '{Operator}' is not of integer type: {Right}");

        return leftType;
    }
}

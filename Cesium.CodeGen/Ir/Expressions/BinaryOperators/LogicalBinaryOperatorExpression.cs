using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class LogicalBinaryOperatorExpression : BinaryOperatorExpression
{
    public LogicalBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if(!Operator.IsLogical())
            throw new AssertException($"Operator {Operator} is not logical.");
    }

    public LogicalBinaryOperatorExpression(Ast.LogicalBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower(IDeclarationScope scope) => new LogicalBinaryOperatorExpression(Left.Lower(scope), Operator, Right.Lower(scope));

    public override IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.Bool;
}

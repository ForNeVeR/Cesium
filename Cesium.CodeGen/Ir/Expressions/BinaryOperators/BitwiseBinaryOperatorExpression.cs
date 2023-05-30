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
}

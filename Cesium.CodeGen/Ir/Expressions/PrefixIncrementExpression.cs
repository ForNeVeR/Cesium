using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class PrefixIncrementExpression : IExpression
{
    private readonly IExpression _target;
    public PrefixIncrementExpression(Ast.PrefixIncrementExpression expression)
    {
        expression.Deconstruct(out var target);
        _target = target.ToIntermediate();
    }

    public IExpression Lower()
    {
        var target = _target.Lower();
        return new AssignmentExpression(
            target,
            BinaryOperator.Assign,
            new ArithmeticBinaryOperatorExpression(
                target,
                BinaryOperator.Add,
                new ConstantExpression(new IntegerConstant("1"))
            )
        );
    }

    public void EmitTo(IDeclarationScope scope) => throw new AssertException("Should be lowered");

    public TypeReference GetExpressionType(IDeclarationScope scope) => _target.GetExpressionType(scope);
}

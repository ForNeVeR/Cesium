using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Constants;

namespace Cesium.CodeGen.Ir.Expressions;

internal class PrefixIncrementExpression : IExpression
{
    private IExpression _target;
    public PrefixIncrementExpression(Ast.PrefixIncrementExpression expression)
    {
        expression.Deconstruct(out var target);
        _target = target.ToIntermediate();
    }

    public IExpression Lower() => new AssignmentExpression(
        _target.Lower(),
        BinaryOperator.Assign,
        new BinaryOperatorExpression(
            _target.Lower(),
            BinaryOperator.Add,
            new ConstantExpression(new IntegerConstant("1"))
        )
    );

    public void EmitTo(FunctionScope scope)
    {
        throw new NotImplementedException();
    }
}

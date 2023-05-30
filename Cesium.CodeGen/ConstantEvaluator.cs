using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;

namespace Cesium.CodeGen;

internal class ConstantEvaluator
{
    private readonly IExpression _expression;

    public ConstantEvaluator(IExpression expression)
    {
        _expression = expression;
    }

    public IConstant GetConstantValue()
    {
        var expression = _expression;

        if (expression is not ConstantLiteralExpression literalExpression)
        {
            throw new AssertException($"Expression {expression} cannot be evaluated as constant expression.");
        }

        return literalExpression.Constant;
    }
}

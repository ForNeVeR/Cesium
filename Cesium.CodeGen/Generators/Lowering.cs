using Cesium.Ast;

namespace Cesium.CodeGen.Generators;

internal static class Lowering // TODO[F]: Remove this class
{
    internal static Expression LowerExpression(Expression expr)
    {
        return expr switch
        {
            PrefixIncrementExpression prefixIncrementExpression => LowerPrefixIncrementExpression(prefixIncrementExpression),
            _ => expr
        };
    }

    private static Expression LowerPrefixIncrementExpression(PrefixIncrementExpression prefixIncrementExpression)
    {
        var constantOne = new IntConstantExpression(1);
        var binaryExpression = new BinaryOperatorExpression(prefixIncrementExpression.Target, "+", constantOne);
        return new AssignmentExpression(prefixIncrementExpression.Target, "=", binaryExpression);
    }
}

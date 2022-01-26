using Cesium.Ast;

namespace Cesium.CodeGen.Generators;

internal static class Lowering // TODO[F]: Remove this class
{
    internal static Expression LowerExpression(Expression expr)
    {
        return expr switch
        {
            AssignmentExpression assignmentExpression => LowerAssignmentExpression(assignmentExpression),
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

    private static Expression LowerAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        switch (assignmentExpression.Operator)
        {
            case "+=":
            case "*=":
                var loweredOperator = LowerOperator(assignmentExpression.Operator);
                var binaryExpression = new BinaryOperatorExpression(assignmentExpression.Left, loweredOperator, assignmentExpression.Right);
                return new AssignmentExpression(assignmentExpression.Left, "=", binaryExpression);
            case "=":
            default:
                return assignmentExpression;
        }
    }

    private static string LowerOperator(string operatorToken)
    {
        return operatorToken switch
        {
            "+=" => "+",
            "*=" => "*",
            _ => throw new NotImplementedException($"Lowering for operator token {operatorToken} is not implemented"),
        };
    }
}

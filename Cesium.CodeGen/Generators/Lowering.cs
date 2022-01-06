using System.Collections.Immutable;
using Cesium.Ast;

namespace Cesium.CodeGen.Generators;

internal static class Lowering
{
    public static CompoundStatement LowerStatement(CompoundStatement statement)
    {
        return new CompoundStatement(Lower(statement.Block));
    }
    public static Statement LowerStatement(Statement statement)
    {
        return statement switch
        {
            ExpressionStatement { Expression: {} } expressionStatement
                => new ExpressionStatement(LowerExpression(expressionStatement.Expression)),
            _ => statement,
        };
    }

    private static ImmutableArray<IBlockItem> Lower(ImmutableArray<IBlockItem> items)
    {
        var result = items;
        for (var i = 0; i < items.Length; i++)
        {
            result = result.SetItem(i, LowerBlockItem(result[i]));
        }

        return result;
    }

    private static IBlockItem LowerBlockItem(IBlockItem item)
    {
        return item switch
        {
            Statement statement => LowerStatement(statement),
            _ => item,
        };
    }

    private static Expression LowerExpression(Expression expr)
    {
        return expr switch 
        {
            AssignmentExpression assignmentExpression => LowerAssignmentExpression(assignmentExpression),
            _ => expr
        };
    }

    private static Expression LowerAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        switch (assignmentExpression.Operator)
        {
            case "+=":
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
            _ => throw new NotImplementedException($"Lowering for operator token {operatorToken} is not implemented"),
        };
    }
} 
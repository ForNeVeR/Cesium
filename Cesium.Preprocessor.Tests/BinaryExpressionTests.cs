using Xunit;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor.Tests;

public class BinaryExpressionTests
{
    [Theory]
    [InlineData("9", CPreprocessorOperator.Equals, "10", false)]
    [InlineData("10", CPreprocessorOperator.Equals, "10", true)]

    [InlineData("9", CPreprocessorOperator.NotEquals, "10", true)]
    [InlineData("10", CPreprocessorOperator.NotEquals, "10", false)]

    [InlineData("9", CPreprocessorOperator.LessOrEqual, "9", true)]
    [InlineData("9", CPreprocessorOperator.LessOrEqual, "10", true)]
    [InlineData("10", CPreprocessorOperator.LessOrEqual, "10", false)]

    [InlineData("9", CPreprocessorOperator.GreaterOrEqual, "9", true)]
    [InlineData("10", CPreprocessorOperator.GreaterOrEqual, "9", true)]
    [InlineData("9", CPreprocessorOperator.GreaterOrEqual, "10", false)]

    [InlineData("10", CPreprocessorOperator.LessThan, "9", false)]
    [InlineData("9", CPreprocessorOperator.LessThan, "10", true)]
    [InlineData("10", CPreprocessorOperator.LessThan, "10", false)]

    [InlineData("10", CPreprocessorOperator.GreaterThan, "9", true)]
    [InlineData("9", CPreprocessorOperator.GreaterThan, "10", false)]
    [InlineData("10", CPreprocessorOperator.GreaterThan, "10", false)]

    [InlineData("0", CPreprocessorOperator.LogicalAnd, "0", false)]
    [InlineData("0", CPreprocessorOperator.LogicalAnd, "1", false)]
    [InlineData("1", CPreprocessorOperator.LogicalAnd, "0", false)]
    [InlineData("1", CPreprocessorOperator.LogicalAnd, "1", true)]

    [InlineData("0", CPreprocessorOperator.LogicalOr, "0", false)]
    [InlineData("0", CPreprocessorOperator.LogicalOr, "1", true)]
    [InlineData("1", CPreprocessorOperator.LogicalOr, "0", true)]
    [InlineData("1", CPreprocessorOperator.LogicalOr, "1", true)]
    public void EvaluateExpressionAllVariants(
        string firstValue,
        CPreprocessorOperator @operator,
        string secondValue,
        bool expectedResult)
    {
        // Arrange
        const string definedName = "TEST";
        var tokens = new List<IToken<CPreprocessorTokenType>>
        {
            new TokenBuilder()
                .WithRange()
                .WithLocation()
                .WithText(firstValue)
                .WithKind(CPreprocessorTokenType.PreprocessingToken)
                .Build()
        };
        var context = new InMemoryDefinesContextBuilder()
            .WithDefineMacro(definedName, tokens)
            .Build();

        var binaryExpression = new BinaryExpression(
            new IdentifierExpression(definedName),
            @operator,
            new IdentifierExpression(secondValue));

        // Act
        var actualResult = binaryExpression.EvaluateExpression(context).ToBoolean();

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}

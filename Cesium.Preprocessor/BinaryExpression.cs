using Cesium.Core;

namespace Cesium.Preprocessor;

internal class BinaryExpression(
    IPreprocessorExpression first,
    CPreprocessorOperator @operator,
    IPreprocessorExpression second)
    : IPreprocessorExpression
{
    private IPreprocessorExpression First { get; } = first;
    private CPreprocessorOperator Operator { get; } = @operator;
    private IPreprocessorExpression Second { get; } = second;

    public string EvaluateExpression(IMacroContext context)
    {
        var firstValue = First.EvaluateExpression(context);
        var parsedFirstValue = Parse(firstValue
                                     ?? throw new PreprocessorException("The left-hand element of the " +
                                                                        "expression was not found"));

        var secondValue = Second.EvaluateExpression(context);
        var parsedSecondValue = Parse(secondValue
                                      ?? throw new PreprocessorException("The right-hand element of the " +
                                                                         "expression was not found"));

        var result = Operator switch
        {
            CPreprocessorOperator.Equals => parsedFirstValue == parsedSecondValue,
            CPreprocessorOperator.NotEquals => parsedFirstValue != parsedSecondValue,
            CPreprocessorOperator.LessOrEqual => parsedFirstValue <= parsedSecondValue,
            CPreprocessorOperator.GreaterOrEqual => parsedFirstValue >= parsedSecondValue,
            CPreprocessorOperator.LessThan => parsedFirstValue < parsedSecondValue,
            CPreprocessorOperator.GreaterThan => parsedFirstValue > parsedSecondValue,
            CPreprocessorOperator.LogicalAnd => parsedFirstValue.AsBoolean() && parsedSecondValue.AsBoolean(),
            CPreprocessorOperator.LogicalOr => parsedFirstValue.AsBoolean() || parsedSecondValue.AsBoolean(),
            _ => throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives"),
        };

        return BooleanToString(result);

        string BooleanToString(bool expressionResult) => expressionResult ? "1" : "0";

        int Parse(string macrosValue)
        {
            if (int.TryParse(macrosValue, out var parsedMacroValue))
                return parsedMacroValue;

            if (macrosValue.StartsWith("0b"))
                return Convert.ToInt32(macrosValue[2..], 2);

            if (macrosValue.StartsWith("0x"))
                return Convert.ToInt32(macrosValue[2..], 16);

            if (macrosValue[0] == '0' && char.IsDigit(macrosValue[1]))
                return Convert.ToInt32(macrosValue[2..], 8);

            throw new NotSupportedException("Invalid macros format");
        }
    }
}

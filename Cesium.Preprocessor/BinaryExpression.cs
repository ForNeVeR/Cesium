using System.Text.RegularExpressions;
using Cesium.Core;
using Yoakke.SynKit.Lexer;

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
        var secondValue = Second.EvaluateExpression(context);

        var parsedFirstValue = firstValue is null ? 0 : Parse(firstValue);
        var parsedSecondValue = secondValue is null ? 0 : Parse(secondValue);

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
            if (Regex.IsMatch(macrosValue, $"^(0|[1-9][0-9]*)$"))
                return int.Parse(macrosValue);

            if (Regex.IsMatch(macrosValue, "^0b[01]+$"))
                return Convert.ToInt32(macrosValue[2..], 2);

            if (Regex.IsMatch(macrosValue, $"^{Regexes.HexLiteral}$"))
                return Convert.ToInt32(macrosValue[2..], 16);

            if (Regex.IsMatch(macrosValue, "^0[0-7]+$"))
                return Convert.ToInt32(macrosValue[1..], 8);

            throw new PreprocessorException("Invalid macros format");
        }
    }
}

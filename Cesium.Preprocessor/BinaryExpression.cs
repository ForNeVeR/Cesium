using Cesium.Core;

namespace Cesium.Preprocessor;

public class BinaryExpression(
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
        return Operator switch
        {
            CPreprocessorOperator.Equals => firstValue == secondValue ? "1" : "0",
            CPreprocessorOperator.NotEquals => firstValue != secondValue ? "1" : "0",
            CPreprocessorOperator.LessOrEqual => (firstValue ?? "").CompareTo(secondValue ?? "") <= 0 ? "1" : "0",
            CPreprocessorOperator.GreaterOrEqual => (firstValue ?? "").CompareTo(secondValue ?? "") >= 0 ? "1" : "0",
            CPreprocessorOperator.LessThan => (firstValue ?? "").CompareTo(secondValue ?? "") < 0 ? "1" : "0",
            CPreprocessorOperator.GreaterThan => (firstValue ?? "").CompareTo(secondValue ?? "") > 0 ? "1" : "0",
            CPreprocessorOperator.LogicalAnd => firstValue.AsBoolean() && secondValue.AsBoolean() ? "1" : "0",
            CPreprocessorOperator.LogicalOr => firstValue.AsBoolean() || secondValue.AsBoolean() ? "1" : "0",
            _ => throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives"),
        };
    }
}

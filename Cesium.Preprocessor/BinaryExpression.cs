using Cesium.Core;

namespace Cesium.Preprocessor;

internal sealed class BinaryExpression : IPreprocessorExpression
{
    public BinaryExpression(IPreprocessorExpression first, CPreprocessorOperator @operator, IPreprocessorExpression second)
    {
        First = first;
        Operator = @operator;
        Second = second;
    }

    public IPreprocessorExpression First { get; }
    public CPreprocessorOperator Operator { get; }
    public IPreprocessorExpression Second { get; }

    public string? EvaluateExpression(IMacroContext context)
    {
        string? firstValue = First.EvaluateExpression(context);
        string? secondValue = Second.EvaluateExpression(context);
        return Operator switch
        {
            CPreprocessorOperator.Equals => firstValue == secondValue ? "1" : "0",
            CPreprocessorOperator.NotEquals => firstValue != secondValue ? "1" : "0",
            CPreprocessorOperator.LessOrEqual => (firstValue ?? "").CompareTo(secondValue ?? "") <= 0 ? "1" : "0",
            CPreprocessorOperator.GreaterOrEqual => (firstValue ?? "").CompareTo(secondValue ?? "") >= 0 ? "1" : "0",
            CPreprocessorOperator.LessThan => (firstValue ?? "").CompareTo(secondValue ?? "") < 0 ? "1" : "0",
            CPreprocessorOperator.GreaterThan => (firstValue ?? "").CompareTo(secondValue ?? "") > 0 ? "1" : "0",
            CPreprocessorOperator.LogicalAnd => (firstValue.AsBoolean() && secondValue.AsBoolean()) ? "1" : "0",
            CPreprocessorOperator.LogicalOr => (firstValue.AsBoolean() || secondValue.AsBoolean()) ? "1" : "0",
            _ => throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives"),
        };
    }
}

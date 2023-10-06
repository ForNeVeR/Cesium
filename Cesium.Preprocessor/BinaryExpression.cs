using Cesium.Core;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

internal class BinaryExpression : IPreprocessorExpression
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
        switch(Operator)
        {
            case CPreprocessorOperator.Equals:
                return firstValue == secondValue ? "1" : "0";
            case CPreprocessorOperator.NotEquals:
                return firstValue != secondValue ? "1" : "0";
            case CPreprocessorOperator.LessOrEqual:
                return (firstValue ?? "").CompareTo(secondValue ?? "") <= 0 ? "1" : "0";
            case CPreprocessorOperator.GreaterOrEqual:
                return (firstValue ?? "").CompareTo(secondValue ?? "") >= 0 ? "1" : "0";
            case CPreprocessorOperator.LessThen:
                return (firstValue ?? "").CompareTo(secondValue ?? "") < 0 ? "1" : "0";
            case CPreprocessorOperator.GreaterThen:
                return (firstValue ?? "").CompareTo(secondValue ?? "") > 0 ? "1" : "0";
            case CPreprocessorOperator.LogicalAnd:
                return (firstValue.AsBoolean() && secondValue.AsBoolean()) ? "1" : "0";
            case CPreprocessorOperator.LogicalOr:
                return (firstValue.AsBoolean() || secondValue.AsBoolean()) ? "1" : "0";
            default:
                throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives");
        }
    }
}

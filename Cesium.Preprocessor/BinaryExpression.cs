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
                return firstValue == secondValue ? "1" : null;
            case CPreprocessorOperator.NotEquals:
                return firstValue != secondValue ? "1" : null;
            case CPreprocessorOperator.LogicalAnd:
                return (!string.IsNullOrEmpty(firstValue) && !string.IsNullOrEmpty(secondValue)) ? "1" : null;
            case CPreprocessorOperator.LogicalOr:
                return (!string.IsNullOrEmpty(firstValue) || !string.IsNullOrEmpty(secondValue)) ? "1" : null;
            default:
                throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives");
        }
    }
}

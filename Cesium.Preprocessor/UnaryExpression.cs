using Cesium.Core;

namespace Cesium.Preprocessor;

internal sealed class UnaryExpression : IPreprocessorExpression
{
    public UnaryExpression(CPreprocessorOperator @operator, IPreprocessorExpression expression)
    {
        Operator = @operator;
        Expression = expression;
    }

    public CPreprocessorOperator Operator { get; }
    public IPreprocessorExpression Expression { get; }

    public string? EvaluateExpression(IMacroContext context)
    {
        string? expressionValue = Expression.EvaluateExpression(context);
        return Operator switch
        {
            CPreprocessorOperator.Negation => !expressionValue.AsBoolean() ? "1" : "0",
            _ => throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives"),
        };
    }
}

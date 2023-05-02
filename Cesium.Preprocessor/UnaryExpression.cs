using Cesium.Core;

namespace Cesium.Preprocessor;

internal class UnaryExpression : IPreprocessorExpression
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
        switch(Operator)
        {
            case CPreprocessorOperator.Negation:
                return !expressionValue.AsBoolean() ? "1" : "0";
            default:
                throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives");
        }
    }
}

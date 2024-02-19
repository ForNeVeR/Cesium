using Cesium.Core;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal sealed record UnaryExpression(
    Location Location,
    CPreprocessorOperator Operator,
    IPreprocessorExpression Expression
) : IPreprocessorExpression
{
    public string EvaluateExpression(IMacroContext context)
    {
        var expressionValue = Expression.EvaluateExpression(context);
        return Operator switch
        {
            CPreprocessorOperator.Negation => !expressionValue.AsBoolean(Location) ? "1" : "0",
            _ => throw new CompilationException($"Operator {Operator} cannot be used in the preprocessor directives")
        };
    }
}

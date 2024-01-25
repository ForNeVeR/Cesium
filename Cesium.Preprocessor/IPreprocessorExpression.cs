namespace Cesium.Preprocessor;

public interface IPreprocessorExpression
{
    string? EvaluateExpression(IMacroContext context);
}

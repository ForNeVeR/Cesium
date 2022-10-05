namespace Cesium.Preprocessor;

internal interface IPreprocessorExpression
{
    string? EvaluateExpression(IMacroContext context);
}

using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal interface IPreprocessorExpression
{
    Location Location { get; }
    string? EvaluateExpression(IMacroContext context);
}

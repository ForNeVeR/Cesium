using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal sealed record DefinedExpression(Location Location, string Identifier) : IPreprocessorExpression
{
    public string EvaluateExpression(IMacroContext context)
    {
        return context.TryResolveMacro(Identifier, out _, out _) ? "1" : "0";
    }
}

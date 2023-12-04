namespace Cesium.Preprocessor;

internal sealed class DefinedExpression : IPreprocessorExpression
{
    public DefinedExpression(string identifer)
    {
        this.Identifer = identifer;
    }

    public string Identifer { get; }

    public string? EvaluateExpression(IMacroContext context)
    {
        if (context.TryResolveMacro(this.Identifer, out var parameters, out var macroReplacement))
        {
            return "1";
        }

        return "0";
    }
}

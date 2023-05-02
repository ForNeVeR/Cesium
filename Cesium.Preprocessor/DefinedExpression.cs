using System.Text.RegularExpressions;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

internal class DefinedExpression : IPreprocessorExpression
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

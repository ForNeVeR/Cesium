using System.Text.RegularExpressions;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

internal sealed class IdentifierExpression : IPreprocessorExpression
{
    public IdentifierExpression(string identifer)
    {
        Identifer = identifer;
    }

    public string Identifer { get; }

    public string? EvaluateExpression(IMacroContext context)
    {
        string? lastValue = null;
        var searchValue = Identifer;
        do
        {
            if (Regex.IsMatch(searchValue, $"^{Regexes.IntLiteral}$")
                || Regex.IsMatch(searchValue, $"^{Regexes.HexLiteral}$")
                || Regex.IsMatch(searchValue, "^0[0-7]+$")
                || Regex.IsMatch(searchValue, "^0b[01]+$"))
            {
                return searchValue;
            }

            if (context.TryResolveMacro(searchValue, out var parameters, out var macroReplacement))
            {
                searchValue = macroReplacement.SkipWhile(_ => _.Kind == CPreprocessorTokenType.WhiteSpace).FirstOrDefault()?.Text ?? "";
                continue;
            }

            return lastValue;
        }
        while (true);
    }
}

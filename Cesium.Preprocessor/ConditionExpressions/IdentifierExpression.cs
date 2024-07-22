using System.Text.RegularExpressions;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal sealed record IdentifierExpression(Location Location, string Identifier) : IPreprocessorExpression
{
    public string? EvaluateExpression(IMacroContext context)
    {
        var searchValue = Identifier;
        do
        {
            if (Regex.IsMatch(searchValue, $"^(0|[1-9][0-9]*)$")
                || Regex.IsMatch(searchValue, $"^{Regexes.HexLiteral}$")
                || Regex.IsMatch(searchValue, "^0[0-7]+$")
                || Regex.IsMatch(searchValue, "^0b[01]+$"))
            {
                return searchValue;
            }

            if (context.TryResolveMacro(searchValue, out _, out var macroReplacement))
            {
                searchValue = macroReplacement.SkipWhile(t => t.Kind == CPreprocessorTokenType.WhiteSpace)
                    .FirstOrDefault()?.Text ?? string.Empty;
                continue;
            }

            return searchValue == string.Empty ? null : "0";
        }
        while (true);
    }
}

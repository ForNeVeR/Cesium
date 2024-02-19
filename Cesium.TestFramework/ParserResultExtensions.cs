using System.Text;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;

namespace Cesium.TestFramework;

public static class ParserResultExtensions
{
    public static string? GetErrorString<T>(this ParseResult<T> result)
    {
        if (!result.IsError) return null;

        var errorMessage = new StringBuilder();
        var err = result.Error;
        foreach (var element in err.Elements.Values)
        {
            errorMessage.AppendLine($"expected {string.Join(" or ", element.Expected)} while parsing {element.Context}");
        }

        var got = err.Got switch
        {
            null => "end of input",
            IToken<CTokenType> t => $"{t.Kind}: {t.Text}",
            var o => o.ToString()
        };
        errorMessage.AppendLine($" but got {got}");

        return errorMessage.ToString();
    }
}

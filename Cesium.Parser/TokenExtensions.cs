using System.Text;
using System.Text.RegularExpressions;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Parser;

public static class TokenExtensions
{
    public static string UnwrapStringLiteral(this IToken<CTokenType> token)
    {
        if (token.Kind != CTokenType.StringLiteral)
            throw new ParseException($"Non-string literal token: {token.Kind} {token.Text}");

        // simple escape sequences
        var result = token.Text.Trim('"')
            .Replace("\\'", "\'")
            .Replace("\\\"", "\"")
            .Replace("\\?", "?")
            .Replace("\\\\", "\\")
            .Replace("\\a", "\a")
            .Replace("\\b", "\b")
            .Replace("\\f", "\f")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\v", "\v");

        // numeric escape sequences
        result = Regex.Replace(result, @"\\([0-7]{3})", m =>
            char.ConvertFromUtf32(Convert.ToInt32(m.Groups[1].Value, 8)));

        result = Regex.Replace(result, @"\\[xX]([0-9a-fA-F]{2})", m =>
            char.ConvertFromUtf32(Convert.ToInt32("0x" + m.Groups[1].Value, 16)));

        //universal character names
        result = Regex.Replace(result, @"\\u([0-9a-fA-F]{4})", m =>
            char.ConvertFromUtf32(Convert.ToInt32("0x" + m.Groups[1].Value, 16)));

        result = Regex.Replace(result, @"\\U([0-9a-fA-F]{8})", m =>
            char.ConvertFromUtf32(Convert.ToInt32("0x" + m.Groups[1].Value, 16)));

        return result;
    }
}

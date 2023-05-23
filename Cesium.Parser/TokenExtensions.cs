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

        var result = token.Text[1..^1];

        // simple escape sequences
        var builder = new StringBuilder(result.Length);
        for (int i = 0; i < result.Length; ++i)
        {
            if (result.ElementAt(i) == '\\' && i < result.Length - 1)
            {
                switch (result.ElementAt(i + 1))
                {
                    case '\'':
                    case '\"':
                    case '?':
                    case '\\':
                        builder.Append(result.ElementAt(i + 1));
                        break;
                    case 'a':
                        builder.Append('\a');
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'v':
                        builder.Append('\v');
                        break;
                    default:
                        // TODO[#295]: maybe smarter handling of this edge case with errors/warnings
                        builder.Append("\\");
                        --i; // don't skip next
                        break;
                }

                ++i; // skip next
            }
            else
            {
                builder.Append(result.ElementAt(i));
            }
        }
        result = builder.ToString();

        // numeric escape sequences
        result = Regex.Replace(result, @"\\([0-7]{1,3})", m =>
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

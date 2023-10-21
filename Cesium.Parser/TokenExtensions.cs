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
                char currentChar = result.ElementAt(i + 1);
                switch (currentChar)
                {
                    case '\'':
                    case '\"':
                    case '?':
                    case '\\':
                        builder.Append(currentChar);
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
                    case '0':
                        {
                            int counter = 2;
                            int octalNumber = 0;
                            if (result.Length <= i + counter)
                            {
                                builder.Append('\0');
                                break;
                            }

                            char current = result.ElementAt(i + counter);
                            do
                            {
                                octalNumber = octalNumber * 8 + (current - '0');
                                counter++;
                                if (result.Length <= i + counter)
                                    break;
                                current = result.ElementAt(i + counter);
                            }
                            while (current >= '0' && current <= '7');
                            i += counter - 1;
                            builder.Append(char.ConvertFromUtf32(octalNumber));
                            break;
                        }
                    case 'x':
                    case 'X':
                        {
                            int counter = 2;
                            int octalNumber = 0;
                            if (result.Length <= i + counter)
                            {
                                builder.Append('\\');
                                builder.Append(currentChar);
                                break;
                            }

                            char current = result.ElementAt(i + counter);
                            do
                            {
                                int digit = current >= '0' && current <= '9' ? current - '0' : (char.ToUpperInvariant(current) - 'A') + 10;
                                octalNumber = octalNumber * 16 + digit;
                                counter++;
                                if (result.Length <= i + counter)
                                    break;
                                current = result.ElementAt(i + counter);
                            }
                            while ((current >= '0' && current <= '9') || (current >= 'A' && current <= 'F') || (current >= 'a' && current <= 'f'));
                            i += counter - 1;
                            builder.Append((char)octalNumber);
                            break;
                        }
                    case 'u':
                    case 'U':
                        {
                            int counter = 2;
                            int octalNumber = 0;
                            if (result.Length <= i + counter)
                            {
                                builder.Append('\\');
                                builder.Append(currentChar);
                                break;
                            }

                            char current = result.ElementAt(i + counter);
                            do
                            {
                                int digit = current >= '0' && current <= '9' ? current - '0' : (char.ToUpperInvariant(current) - 'A') + 10;
                                octalNumber = octalNumber * 16 + digit;
                                counter++;
                                if (result.Length <= i + counter)
                                    break;
                                current = result.ElementAt(i + counter);
                            }
                            while ((current >= '0' && current <= '9') || (current >= 'A' && current <= 'F') || (current >= 'a' && current <= 'f'));
                            i += counter - 1;
                            builder.Append(char.ConvertFromUtf32(octalNumber));
                            break;
                        }
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

        //universal character names
        //result = Regex.Replace(result, @"\\u([0-9a-fA-F]{4})", m =>
        //    char.ConvertFromUtf32(Convert.ToInt32("0x" + m.Groups[1].Value, 16)));

        //result = Regex.Replace(result, @"\\U([0-9a-fA-F]{8})", m =>
        //    char.ConvertFromUtf32(Convert.ToInt32("0x" + m.Groups[1].Value, 16)));

        return result;
    }
}

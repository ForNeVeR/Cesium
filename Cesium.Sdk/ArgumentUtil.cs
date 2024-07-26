using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cesium.Sdk;

public static class ArgumentUtil
{
    public static string ToCommandLineString(IEnumerable<string> args)
    {
        var result = new StringBuilder();
        var first = true;
        foreach (var a in args)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                result.Append(' ');
            }
            if (a.Length == 0 || a.Any(c => char.IsWhiteSpace(c) || c == '"'))
            {
                result.Append(Quoted(a));
            }
            else
            {
                result.Append(a);
            }
        }
        return result.ToString();
    }

    private static string Quoted(string arg)
    {
        // The simplified rules:
        // 1. Every quote should be escaped by \
        // 2. Slashes preceding the quotes should be escaped by \
        //
        // Meaning any amount of slashes following a quote should be converted to twice as many slashes + slash + quote.
        // The final quote (not part of the argument per se) also counts as quote for this purpose.
        var result = new StringBuilder(arg.Length + 2);
        result.Append('"');
        var slashes = 0;
        foreach (var c in arg)
        {
            switch (c)
            {
                case '\\':
                    slashes++;
                    break;
                case '"':
                    result.Append('\\', slashes * 2);
                    slashes = 0;

                    result.Append("\\\"");
                    break;
                default:
                    result.Append('\\', slashes);
                    slashes = 0;

                    result.Append(c);
                    break;
            }
        }

        result.Append('\\', slashes * 2);
        result.Append('"');

        return result.ToString();
    }
}

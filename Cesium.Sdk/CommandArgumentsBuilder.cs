using System.Linq;
using System.Text;

namespace Cesium.Sdk;

// Implementation reference:
// https://github.com/Tyrrrz/CliWrap/blob/417aaffe171b9897799ed2a28a6467c11d69c296/CliWrap/Builders/ArgumentsBuilder.cs
// MIT License, Oleksii Holub
public class CommandArgumentsBuilder
{
    private StringBuilder _builder = new StringBuilder();

    public CommandArgumentsBuilder Argument(string argument)
    {
        _builder.Append(" ");
        if (NeedsEscaping(argument))
        {
            argument = Escape(argument);
        }
        _builder.Append(argument);

        return this;
    }

    public string Build() => _builder.ToString();

    private bool NeedsEscaping(string argument) =>
        argument.Length > 0 && argument.All(c => !char.IsWhiteSpace(c) && c != '"');

    private static string Escape(string argument)
    {
        var buffer = new StringBuilder();

        buffer.Append('"');

        for (var i = 0; i < argument.Length;)
        {
            var c = argument[i++];

            if (c == '\\')
            {
                var backslashCount = 1;
                while (i < argument.Length && argument[i] == '\\')
                {
                    backslashCount++;
                    i++;
                }

                if (i == argument.Length)
                {
                    buffer.Append('\\', backslashCount * 2);
                }
                else if (argument[i] == '"')
                {
                    buffer
                        .Append('\\', backslashCount * 2 + 1)
                        .Append('"');

                    i++;
                }
                else
                {
                    buffer.Append('\\', backslashCount);
                }
            }
            else if (c == '"')
            {
                buffer.Append('\\').Append('"');
            }
            else
            {
                buffer.Append(c);
            }
        }

        buffer.Append('"');

        return buffer.ToString();
    }
}

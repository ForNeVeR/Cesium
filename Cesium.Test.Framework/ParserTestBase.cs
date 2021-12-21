using System.Text;
using Newtonsoft.Json;
using Yoakke.C.Syntax;
using Yoakke.Lexer;
using Yoakke.Parser;

namespace Cesium.Test.Framework;

public abstract class ParserTestBase : VerifyTestBase
{
    protected static string? GetErrorString<T>(ParseResult<T> result)
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

    private static readonly JsonSerializerSettings SerializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new TokenConverter<CTokenType>() }
    };

    protected static string JsonSerialize(object value) => JsonConvert.SerializeObject(value, SerializerOptions);
}

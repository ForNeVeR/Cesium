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
        return result.GetErrorString();
    }

    private static readonly JsonSerializerSettings SerializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new TokenConverter<CTokenType>() },
        TypeNameHandling = TypeNameHandling.Objects
    };

    protected static string JsonSerialize(object value) => JsonConvert.SerializeObject(value, SerializerOptions);
}

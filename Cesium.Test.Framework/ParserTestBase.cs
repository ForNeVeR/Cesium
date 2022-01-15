using Newtonsoft.Json;
using Yoakke.C.Syntax;

namespace Cesium.Test.Framework;

public abstract class ParserTestBase : VerifyTestBase
{
    private static readonly JsonSerializerSettings SerializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new TokenConverter<CTokenType>() },
        TypeNameHandling = TypeNameHandling.Objects
    };

    protected static string JsonSerialize(object value) => JsonConvert.SerializeObject(value, SerializerOptions);
}

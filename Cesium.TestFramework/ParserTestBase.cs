using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.TestFramework;

public abstract class ParserTestBase : VerifyTestBase
{
    private static readonly JsonSerializerSettings SerializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new TokenConverter<CTokenType>(), new StringEnumConverter() },
        TypeNameHandling = TypeNameHandling.Objects
    };

    protected static string JsonSerialize(object value) => JsonConvert.SerializeObject(value, SerializerOptions);
}

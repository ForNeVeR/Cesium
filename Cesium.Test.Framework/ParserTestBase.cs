using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.Test.Framework;

public abstract class ParserTestBase : VerifyTestBase
{
    private static readonly JsonSerializerSettings SerializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new TokenConverter<CTokenType>(), new StringEnumConverter() },
        TypeNameHandling = TypeNameHandling.Objects
    };

    protected static string JsonSerialize(object value) =>
        System.Text.Json.JsonSerializer.Serialize(value);
    //JsonConvert.SerializeObject(value, SerializerOptions);
}

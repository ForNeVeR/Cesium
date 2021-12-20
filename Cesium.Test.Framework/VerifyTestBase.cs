using Newtonsoft.Json;
using Yoakke.C.Syntax;

namespace Cesium.Test.Framework;

[UsesVerify]
public class VerifyTestBase
{
    static VerifyTestBase()
    {
        // To disable Visual Studio popping up on every test execution.
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
    }

    private static readonly JsonSerializerSettings SerializerOptions = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new TokenConverter<CTokenType>() }
    };

    public static string JsonSerialize(object value) => JsonConvert.SerializeObject(value, SerializerOptions);
}

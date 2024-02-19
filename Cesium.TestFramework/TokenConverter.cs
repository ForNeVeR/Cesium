using Newtonsoft.Json;
using Yoakke.SynKit.Lexer;

namespace Cesium.TestFramework;

public class TokenConverter<T> : JsonConverter<IToken<T>> where T : Enum
{
    public override void WriteJson(JsonWriter writer, IToken<T>? value, JsonSerializer serializer)
    {
        if (value == null)
            writer.WriteNull();
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue(value.Kind.ToString());
            writer.WritePropertyName("Text");
            writer.WriteValue(value.Text);
            writer.WriteEndObject();
        }
    }

    public override IToken<T> ReadJson(
        JsonReader reader,
        Type objectType,
        IToken<T>? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }
}

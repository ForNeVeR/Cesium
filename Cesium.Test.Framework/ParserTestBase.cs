using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Test.Framework;

public abstract class ParserTestBase : VerifyTestBase
{
    private static readonly JsonSerializerOptions SerializerOptions;

    static ParserTestBase()
    {
        SerializerOptions = new()
        {
            WriteIndented = true,
            Converters = { new TokenConverter(), new JsonStringEnumConverter() }
        };

        SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        {
            Modifiers = { Modifier }
        };
    }

    private static void Modifier(JsonTypeInfo jsonTypeInfo)
    {
        if (!jsonTypeInfo.Type.IsEnum && jsonTypeInfo.Type.AssemblyQualifiedName?.StartsWith("Cesium.Ast") == true)
        {
            foreach (var derivedType in GetDerivedTypes(jsonTypeInfo.Type))
            {
                jsonTypeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions();
                jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(derivedType, derivedType.FullName!));
            }
            if (!jsonTypeInfo.Type.IsInterface && !jsonTypeInfo.Type.IsAbstract && !jsonTypeInfo.Type.IsSealed)
            {
                jsonTypeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions();
                jsonTypeInfo.PolymorphismOptions?.DerivedTypes.Add(new JsonDerivedType(jsonTypeInfo.Type, jsonTypeInfo.Type.FullName!));
            }
        }
    }

    private static IEnumerable<Type> GetDerivedTypes(Type type)
    {
        var allTypes = type.Assembly.GetTypes();

        foreach (var t in allTypes)
            if (t is { IsInterface: false, IsAbstract: false } && t != type && type.IsAssignableFrom(t))
                yield return t;
    }

    protected static string JsonSerialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);


    private class TokenConverter : JsonConverter<IToken<CTokenType>>
    {
        public override IToken<CTokenType>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IToken<CTokenType> value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteString("Kind"u8, value.Kind.ToString());
            writer.WriteString("Text"u8, value.Text);
            writer.WriteEndObject();
        }
    }
}

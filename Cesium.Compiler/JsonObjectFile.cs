// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;
using Cesium.CodeGen;
using Cesium.Core;
using TruePath;

namespace Cesium.Compiler;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    Converters = [typeof(LocalPathConverter)],
    UseStringEnumConverter = true)
]
[JsonSerializable(typeof(JsonObjectFile.CompiledObjectJson))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
    private class LocalPathConverter : JsonConverter<LocalPath>
    {
        public override LocalPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString() ?? throw new InvalidOperationException("Local path not defined.");
            return new LocalPath(value);
        }

        public override void Write(Utf8JsonWriter writer, LocalPath value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}

public static class JsonObjectFile
{
    public class CompiledObjectJson
    {
        public required string[] InputFilePaths { get; init; }
        public required CompilationOptions CompilationOptions { get; init; }

        protected bool Equals(CompiledObjectJson other)
        {
            return InputFilePaths.SequenceEqual(other.InputFilePaths)
                   && CompilationOptions.Equals(other.CompilationOptions);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((CompiledObjectJson)obj);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(CompilationOptions);
            foreach (var inputPath in InputFilePaths)
            {
                hash.Add(inputPath);
            }

            return hash.ToHashCode();
        }
    }

    public static bool IsSupportedExtension(LocalPath path) => path.GetExtensionWithDot() == ".obj" || path.GetExtensionWithDot() == ".o";

    public static async Task Write(
        IEnumerable<LocalPath> inputFilePaths,
        CompilationOptions compilationOptions,
        AbsolutePath outputFile)
    {
        var compiledObjectJson = new CompiledObjectJson
        {
            InputFilePaths = inputFilePaths.Select(x => x.Value).ToArray(),
            CompilationOptions = compilationOptions
        };

        await using var stream = new FileStream(outputFile.Value, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(
            stream,
            compiledObjectJson,
            SourceGenerationContext.Default.CompiledObjectJson);
    }

    public static async Task<CompiledObjectJson> Read(AbsolutePath objectFile)
    {
        await using var stream = new FileStream(objectFile.Value, FileMode.Open, FileAccess.Read);
        var result = JsonSerializer.Deserialize<CompiledObjectJson>(
            stream,
            SourceGenerationContext.Default.CompiledObjectJson);
        if (result == null)
        {
            throw new CompilationException($"Invalid JSON object file \"{objectFile.Value}\".");
        }
        return result;
    }
}

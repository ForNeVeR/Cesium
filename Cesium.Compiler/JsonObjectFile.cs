using System.Text.Json;
using Cesium.CodeGen;
using TruePath;

namespace Cesium.Compiler;

public static class JsonObjectFile
{
    public class CompiledObjectJson
    {
        public required IEnumerable<string> InputFilePaths;
        public required CompilationOptions CompilationOptions;
    }

    public static bool IsCorrectExtension(LocalPath path) => path.GetExtensionWithDot() == ".obj";

    public static async Task<int> Write(
        IEnumerable<string> inputFilePaths,
        string outputFilePath,
        CompilationOptions compilationOptions
    )
    {
        CompiledObjectJson compiledObjectJson = new CompiledObjectJson()
        {
            InputFilePaths = inputFilePaths,
            CompilationOptions = compilationOptions
        };

        if (!outputFilePath.EndsWith(".obj"))
        {
            outputFilePath += ".json.obj";
        }

        StreamWriter outObjectWriter = new StreamWriter(outputFilePath);
        await outObjectWriter.WriteAsync(JsonSerializer.Serialize(compiledObjectJson));
        return 0;
    }

    public static async Task<CompiledObjectJson> Read(AbsolutePath inputObjectJsonFilePath)
    {
        StreamReader inObjectJsonReader = new StreamReader(inputObjectJsonFilePath.Value);

        var inObjectJsonStr = await inObjectJsonReader.ReadToEndAsync();
        var result = JsonSerializer.Deserialize<CompiledObjectJson>(inObjectJsonStr);
        if (result == null)
        {
            throw new Exception($"Invalid json from file {inputObjectJsonFilePath}");
        }
        return result;
    }
}

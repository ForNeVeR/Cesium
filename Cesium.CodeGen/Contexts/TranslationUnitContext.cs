using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(ModuleDefinition Module)
{
    public Dictionary<string, MethodDefinition> Functions { get; } = new();
}

using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(ModuleDefinition Module)
{
    public AssemblyDefinition Assembly => Module.Assembly;
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    public Dictionary<string, MethodReference> Functions { get; } = new();
}

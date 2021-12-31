using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(AssemblyContext AssemblyContext)
{
    public AssemblyDefinition Assembly => AssemblyContext.Assembly;
    public ModuleDefinition Module => AssemblyContext.Module;
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    public Dictionary<string, MethodReference> Functions { get; } = new();
}

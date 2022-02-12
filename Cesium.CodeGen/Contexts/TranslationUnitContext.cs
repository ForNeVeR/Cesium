using Cesium.CodeGen.Contexts.Meta;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(AssemblyContext AssemblyContext)
{
    public AssemblyDefinition Assembly => AssemblyContext.Assembly;
    public ModuleDefinition Module => AssemblyContext.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    internal Dictionary<string, FunctionInfo> Functions => AssemblyContext.Functions;
    internal Dictionary<string, TypeReference> Types { get; } = new();
}

using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(AssemblyContext AssemblyContext)
{
    public AssemblyDefinition Assembly => AssemblyContext.Assembly;
    public ModuleDefinition Module => AssemblyContext.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    internal Dictionary<string, FunctionInfo> Functions => AssemblyContext.Functions;

    private readonly Dictionary<IGeneratedType, TypeReference> _generatedTypes = new();
    private readonly Dictionary<string, TypeReference> _types = new();

    internal void GenerateType(IGeneratedType type, string name)
    {
        var typeReference = type.Emit(name, this);
        _generatedTypes.Add(type, typeReference);
        _types.Add(name, typeReference);
    }

    internal TypeReference? GetTypeReference(IGeneratedType type) => _generatedTypes.GetValueOrDefault(type);
    internal TypeReference? GetTypeReference(string typeName) => _types.GetValueOrDefault(typeName);
}

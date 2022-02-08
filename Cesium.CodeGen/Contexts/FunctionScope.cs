using Cesium.CodeGen.Contexts.Meta;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record FunctionScope(TranslationUnitContext Context, MethodDefinition Method)
{
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public TypeSystem TypeSystem => Context.TypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Context.Functions;

    public Dictionary<string, VariableDefinition> Variables { get; } = new();

    private readonly Dictionary<string, ParameterDefinition> _parameterCache = new();
    public ParameterDefinition? GetParameter(string name)
    {
        if (_parameterCache.TryGetValue(name, out var parameter))
            return parameter;

        parameter = Method.Parameters.FirstOrDefault(p => p.Name == name);
        if (parameter != null) _parameterCache.Add(name, parameter);
        return parameter;
    }
}

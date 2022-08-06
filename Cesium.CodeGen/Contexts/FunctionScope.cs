using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record FunctionScope(TranslationUnitContext Context, MethodDefinition Method) : IDeclarationScope
{
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public TypeSystem TypeSystem => Context.TypeSystem;
    public CTypeSystem CTypeSystem => Context.CTypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Context.Functions;

    private readonly Dictionary<string, VariableDefinition> _variables = new();
    public IReadOnlyDictionary<string, VariableDefinition> Variables => _variables;
    public void AddVariable(string identifier, VariableDefinition variable) => _variables.Add(identifier, variable);

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

using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core.Exceptions;
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

    private readonly Dictionary<string, IType> _variables = new();
    private readonly Dictionary<string, VariableDefinition> _variableDefinition = new();
    public IReadOnlyDictionary<string, IType> Variables => _variables;
    public void AddVariable(string identifier, IType variable) => _variables.Add(identifier, variable);
    public VariableDefinition ResolveVariable(string identifier)
    {
        if (!_variables.TryGetValue(identifier, out var variableType))
        {
            throw new CompilationException($"Identifier {identifier} was not found in the {Method} scope");
        }

        if (!_variableDefinition.TryGetValue(identifier, out var variableDefinition))
        {
            var typeReference = variableType.Resolve(Context);
            variableDefinition = new VariableDefinition(typeReference);
            Method.Body.Variables.Add(variableDefinition);
            _variableDefinition.Add(identifier, variableDefinition);
        }

        return variableDefinition;
    }

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

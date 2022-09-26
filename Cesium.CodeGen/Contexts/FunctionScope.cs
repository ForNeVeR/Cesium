using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics.CodeAnalysis;

namespace Cesium.CodeGen.Contexts;

internal record FunctionScope(TranslationUnitContext Context, FunctionInfo FunctionInfo, MethodDefinition Method) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public TypeSystem TypeSystem => Context.TypeSystem;
    public CTypeSystem CTypeSystem => Context.CTypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Context.Functions;
    public bool TryGetFunctionInfo(string identifier, [NotNullWhen(true)] out FunctionInfo? functionInfo)
    {
        return Context.Functions.TryGetValue(identifier, out functionInfo);
    }

    private readonly Dictionary<string, IType> _variables = new();
    private readonly Dictionary<string, Instruction> _labels = new();
    private readonly Dictionary<string, VariableDefinition> _variableDefinition = new();
    public IReadOnlyDictionary<string, IType> GlobalFields => AssemblyContext.GlobalFields;
    public void AddVariable(string identifier, IType variable) => _variables.Add(identifier, variable);

    public bool TryGetVariable(string identifier, [NotNullWhen(true)] out IType? type)
    {
        return _variables.TryGetValue(identifier, out type);
    }

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
    public ParameterInfo? GetParameterInfo(string name) => FunctionInfo.Parameters?.Parameters.FirstOrDefault(p => p.Name == name);

    private readonly Dictionary<string, ParameterDefinition> _parameterCache = new();
    public ParameterDefinition ResolveParameter(string name)
    {
        if (_parameterCache.TryGetValue(name, out var parameter))
            return parameter;

        parameter = Method.Parameters.FirstOrDefault(p => p.Name == name) ?? throw new AssertException($"Cannot resolve parameter with name name {name}");
        _parameterCache.Add(name, parameter);
        return parameter;
    }
    /// <inheritdoc />
    public IType ResolveType(IType type) => Context.ResolveType(type);
    public void AddTypeDefinition(string identifier, IType type) => throw new AssertException("Not supported");

    /// <inheritdoc />
    public void AddLabel(string identifier)
    {
        if (_labels.ContainsKey(identifier))
        {
            throw new CompilationException($"Label {identifier} was already registered.");
        }

        _labels.Add(identifier, Instruction.Create(OpCodes.Nop));
    }

    /// <inheritdoc />
    public Instruction ResolveLabel(string label)
    {
        return _labels[label];
    }
}

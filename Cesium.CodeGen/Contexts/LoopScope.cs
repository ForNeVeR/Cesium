using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record LoopScope(IEmitScope Parent) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public CTypeSystem CTypeSystem => Parent.CTypeSystem;
    public FunctionInfo? GetFunctionInfo(string identifier)
        => ((IDeclarationScope)Parent).GetFunctionInfo(identifier);
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;

    private readonly Dictionary<string, IType> _variables = new();
    private readonly Dictionary<string, VariableDefinition> _variableDefinition = new();

    public IType? GetVariable(string identifier)
    {
        return _variables.TryGetValue(identifier, out var variable)
            ? variable
            : ((IDeclarationScope)Parent).GetVariable(identifier);
    }
    public IReadOnlyDictionary<string, IType> GlobalFields => ((IDeclarationScope)Parent).GlobalFields;
    public void AddVariable(string identifier, IType variable) => _variables.Add(identifier, variable);
    public VariableDefinition ResolveVariable(string identifier)
    {
        if (!_variables.TryGetValue(identifier, out var variableType))
        {
            return Parent.ResolveVariable(identifier);
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

    public ParameterDefinition ResolveParameter(string name) => Parent.ResolveParameter(name);
    public ParameterInfo? GetParameterInfo(string name) => ((IDeclarationScope)Parent).GetParameterInfo(name);

    /// <inheritdoc />
    public IType ResolveType(IType type) => Context.ResolveType(type);
    public void AddTypeDefinition(string identifier, IType type) => throw new AssertException("Not supported");

    /// <inheritdoc />
    public void AddLabel(string identifier)
    {
        ((IDeclarationScope)Parent).AddLabel(identifier);
    }

    /// <inheritdoc />
    public Instruction ResolveLabel(string label)
    {
        return Parent.ResolveLabel(label);
    }

    private string _breakLabel = Guid.NewGuid().ToString();
    private string _continueLabel = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string GetBreakLabel() => _breakLabel;

    /// <inheritdoc />
    public string GetContinueLabel() => _continueLabel;
}

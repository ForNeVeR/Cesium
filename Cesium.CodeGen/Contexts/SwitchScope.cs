using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record SwitchCase(IExpression? TestExpression, string Label);

internal record SwitchScope(IEmitScope Parent) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public CTypeSystem CTypeSystem => Parent.CTypeSystem;
    public TargetArchitectureSet ArchitectureSet => AssemblyContext.ArchitectureSet;
    public FunctionInfo? GetFunctionInfo(string identifier)
        => ((IDeclarationScope)Parent).GetFunctionInfo(identifier);

    public void DeclareFunction(string identifier, FunctionInfo functionInfo)
        => ((IDeclarationScope)Parent).DeclareFunction(identifier, functionInfo);
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;

    private readonly Dictionary<string, VariableInfo> _variables = new();
    private readonly Dictionary<string, VariableDefinition> _variableDefinition = new();

    public VariableInfo? GetVariable(string identifier)
    {
        return _variables.TryGetValue(identifier, out var variable)
            ? variable
            : ((IDeclarationScope)Parent).GetVariable(identifier);
    }
    public IReadOnlyDictionary<string, IType> GlobalFields => ((IDeclarationScope)Parent).GlobalFields;
    public void AddVariable(StorageClass storageClass, string identifier, IType variable)
        => _variables.Add(identifier, new(identifier, storageClass, variable));
    public VariableDefinition ResolveVariable(string identifier)
    {
        if (!_variables.TryGetValue(identifier, out var variableType))
        {
            return Parent.ResolveVariable(identifier);
        }

        if (!_variableDefinition.TryGetValue(identifier, out var variableDefinition))
        {
            var typeReference = variableType.Type.Resolve(Context);
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
    public IType? TryGetType(string identifier) => Context.TryGetType(identifier);
    public void AddTypeDefinition(string identifier, IType type) => throw new AssertException("Not supported");
    public void AddTagDefinition(string identifier, IType type) => throw new AssertException("Not supported");

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

    private string _breakLabel = $"switch_{Guid.NewGuid()}";

    /// <inheritdoc />
    public string GetBreakLabel()
    {
        return _breakLabel;
    }

    /// <inheritdoc />
    public string? GetContinueLabel() => null;

    public List<SwitchCase> SwitchCases { get; } = new();
}

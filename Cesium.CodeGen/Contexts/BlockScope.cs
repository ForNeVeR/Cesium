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

internal record BlockScope(IEmitScope Parent, string? BreakLabel, string? ContinueLabel, List<SwitchCase>? OwnSwitchCases = null) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
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

    public VariableInfo? GetGlobalField(string identifier) => ((IDeclarationScope)Parent).GetGlobalField(identifier);

    public void AddVariable(StorageClass storageClass, string identifier, IType variable, IExpression? constant)
    {
        // quirk - passing Static variables to the parent
        // TODO[#410]: we need more tests for that

        switch (storageClass)
        {
            case StorageClass.Auto:
                _variables.Add(identifier, new(identifier, storageClass, variable, constant));
                break;
            case StorageClass.Static:
                ((IDeclarationScope) Parent).AddVariable(storageClass, identifier, variable, constant);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storageClass), storageClass, null);
        }
    }

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

    public ParameterDefinition ResolveParameter(int index) => Parent.ResolveParameter(index);
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

    /// <inheritdoc />
    public string? GetBreakLabel() => BreakLabel ?? (Parent as IDeclarationScope)?.GetBreakLabel();

    /// <inheritdoc />
    public string? GetContinueLabel() => ContinueLabel ?? (Parent as IDeclarationScope)?.GetContinueLabel();

    public List<SwitchCase>? SwitchCases => OwnSwitchCases ?? (Parent as IDeclarationScope)?.SwitchCases;

    public void PushSpecialEffect(object blockItem) { }

    public T? GetSpecialEffect<T>() => default;

    public void RemoveSpecialEffect<T>(Predicate<T> predicate) { }
}

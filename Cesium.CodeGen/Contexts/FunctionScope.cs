using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Contexts;

internal record FunctionScope(TranslationUnitContext Context, FunctionInfo FunctionInfo, MethodDefinition Method) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public TargetArchitectureSet ArchitectureSet => AssemblyContext.ArchitectureSet;
    public FunctionInfo? GetFunctionInfo(string identifier) =>
        Context.GetFunctionInfo(identifier);

    public void DeclareFunction(string identifier, FunctionInfo functionInfo)
        => Context.DeclareFunction(identifier, functionInfo);

    private readonly Dictionary<string, VariableInfo> _variables = new();
    private readonly Dictionary<string, Instruction> _labels = new();
    private readonly Dictionary<int, VariableDefinition> _variableDefinition = new();
    public VariableInfo? GetGlobalField(string identifier) => AssemblyContext.GetGlobalField(identifier);
    public void AddVariable(StorageClass storageClass, string identifier, IType variableType, IExpression? constant)
    {
        _variables.Add(identifier, new(storageClass, variableType, constant));
        if (storageClass == StorageClass.Static)
        {
            Context.AddTranslationUnitLevelField(storageClass, identifier, variableType);
        }
    }

    public VariableInfo? GetVariable(string identifier)
    {
        VariableInfo? variableInfo = _variables.GetValueOrDefault(identifier);
        if (variableInfo is not null)
        {
            return variableInfo;
        }

        return Context.GetInitializerScope().GetVariable(identifier);
    }

    public VariableDefinition ResolveVariable(int varIndex)
    {
        var variableType = _variables.FirstOrDefault(_ => _.Value.Index == varIndex).Value;
        if (variableType is null)
        {
            throw new CompilationException($"Identifier {varIndex} was not found in the {Method} scope");
        }

        if (!_variableDefinition.TryGetValue(varIndex, out var variableDefinition))
        {
            var typeReference = variableType.Type.Resolve(Context);
            variableDefinition = new VariableDefinition(typeReference);
            Method.Body.Variables.Add(variableDefinition);
            _variableDefinition.Add(varIndex, variableDefinition);
        }

        return variableDefinition;
    }
    public ParameterInfo? GetParameterInfo(string name)
    {
        var parametersInfo = FunctionInfo.Parameters;
        if (parametersInfo is null) return null;
        if (name == "__varargs" && parametersInfo.IsVarArg)
        {
            return new ParameterInfo(new PointerType(CTypeSystem.Void), name, parametersInfo.Parameters.Count);
        }

        return parametersInfo.Parameters.FirstOrDefault(p => p.Name == name);
    }

    public ParameterDefinition ResolveParameter(int index)
    {
        return Method.Parameters[index];
    }
    /// <inheritdoc />
    public IType ResolveType(IType type) => Context.ResolveType(type);
    public IType? TryGetType(string identifier) => Context.TryGetType(identifier);
    public void AddTypeDefinition(string identifier, IType type) => throw new AssertException("Not supported");
    public void AddTagDefinition(string identifier, IType type) => throw new AssertException("Not supported");

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

    /// <inheritdoc />
    public string? GetBreakLabel() => null;

    /// <inheritdoc />
    public string? GetContinueLabel() => null;

    public List<SwitchCase>? SwitchCases => null;

    /// <inheritdoc />
    public void PushPragma(IPragma pragma) { }

    /// <inheritdoc />
    public T? GetPragma<T>() where T : IPragma => default;

    /// <inheritdoc />
    public void RemovePragma<T>(Predicate<T> predicate) where T : IPragma { }

    public void MergeScope(BlockScope scope)
    {
        foreach (var (variableName, variable) in scope.Variables)
        {
            var currentKey = variableName;
            int i = 0;
            while (_variables.ContainsKey(currentKey))
            {
                currentKey = variableName + "_" + i++;
            }

            _variables.Add(currentKey, variable);
        }
        foreach (var (variableName, variableDefinition) in scope.VariableDefinitions)
        {
            _variableDefinition.Add(variableName, variableDefinition);
        }
    }
}

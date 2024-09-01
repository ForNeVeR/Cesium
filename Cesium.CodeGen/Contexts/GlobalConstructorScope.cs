using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal sealed record GlobalConstructorScope(TranslationUnitContext Context) : IEmitScope, IDeclarationScope
{
    private MethodDefinition? _method;
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public MethodDefinition Method => _method ??= Context.AssemblyContext.GetGlobalInitializer();
    public TargetArchitectureSet ArchitectureSet => AssemblyContext.ArchitectureSet;
    public FunctionInfo? GetFunctionInfo(string identifier) =>
        Context.GetFunctionInfo(identifier);

    public void DeclareFunction(string identifier, FunctionInfo functionInfo)
        => Context.DeclareFunction(identifier, functionInfo);
    public VariableInfo? GetGlobalField(string identifier) => AssemblyContext.GetGlobalField(identifier);

    private readonly Dictionary<string, VariableInfo> _variables = new();

    private readonly List<object> _pragmaStack = new();

    public void AddVariable(StorageClass storageClass, string identifier, IType variableType, IExpression? constant)
    {
        if (constant is not null)
        {
            _variables.Add(identifier, new(storageClass, variableType, constant));
            return;
        }

        if (storageClass == StorageClass.Static)
        {
            _variables.Add(identifier, new(storageClass, variableType, constant));
        }

        Context.AddTranslationUnitLevelField(storageClass, identifier, variableType);
    }

    public VariableInfo? GetVariable(string identifier)
    {
        return _variables.GetValueOrDefault(identifier);
    }
    public VariableDefinition ResolveVariable(int varIndex) =>
        throw new AssertException("Cannot resolve a variable into a global constructor scope");

    public ParameterInfo? GetParameterInfo(string name) => null;
    public ParameterDefinition ResolveParameter(int index) =>
        throw new AssertException("Cannot resolve parameter from the global constructor scope");

    /// <inheritdoc />
    public IType ResolveType(IType type) => Context.ResolveType(type);
    public IType? TryGetType(string identifier) => Context.TryGetType(identifier);
    public void AddTypeDefinition(string identifier, IType type) => Context.AddTypeDefinition(identifier, type);
    public void AddTagDefinition(string identifier, IType type) => Context.AddTagDefinition(identifier, type);

    /// <inheritdoc />
    public void AddLabel(string identifier)
    {
        throw new AssertException("Cannot define label into a global constructor scope");
    }

    /// <inheritdoc />
    public Instruction ResolveLabel(string label)
    {
        throw new AssertException("Cannot define label into a global constructor scope");
    }

    /// <inheritdoc />
    public string? GetBreakLabel() => null;

    /// <inheritdoc />
    public string? GetContinueLabel() => null;

    public List<SwitchCase>? SwitchCases => null;

    /// <inheritdoc />
    public void PushPragma(IPragma declaration) => _pragmaStack.Add(declaration);

    /// <inheritdoc />
    public T? GetPragma<T>() where T : IPragma => _pragmaStack.OfType<T>().LastOrDefault();

    /// <inheritdoc />
    public void RemovePragma<T>(Predicate<T> predicate) where T : IPragma
    {
        for (int i = _pragmaStack.Count - 1; i >= 0; i--)
        {
            var effect = _pragmaStack[i];
            if (effect is T t && predicate(t))
            {
                _pragmaStack.RemoveAt(i);
                return;
            }
        }
    }
}

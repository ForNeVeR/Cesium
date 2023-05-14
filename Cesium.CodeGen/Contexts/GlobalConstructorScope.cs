using System.Collections.Immutable;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static System.Formats.Asn1.AsnWriter;

namespace Cesium.CodeGen.Contexts;

internal record GlobalConstructorScope(TranslationUnitContext Context) : IEmitScope, IDeclarationScope
{
    private MethodDefinition? _method;
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public MethodDefinition Method => _method ??= Context.AssemblyContext.GetGlobalInitializer();
    public CTypeSystem CTypeSystem => Context.CTypeSystem;
    public TargetArchitectureSet ArchitectureSet => AssemblyContext.ArchitectureSet;
    public FunctionInfo? GetFunctionInfo(string identifier) =>
        Context.GetFunctionInfo(identifier);

    public void DeclareFunction(string identifier, FunctionInfo functionInfo)
        => Context.DeclareFunction(identifier, functionInfo);
    public IReadOnlyDictionary<string, IType> GlobalFields => AssemblyContext.GlobalFields;

    private readonly Dictionary<string, VariableInfo> _variables = new();

    public void AddVariable(StorageClass storageClass, string identifier, IType variableType)
    {
        if (storageClass == StorageClass.Static)
        {
            _variables.Add(identifier, new(identifier, storageClass, variableType));
        }

        Context.AddTranslationUnitLevelField(storageClass, identifier, variableType);
    }

    public VariableInfo? GetVariable(string identifier)
    {
        return _variables.GetValueOrDefault(identifier);
    }
    public VariableDefinition ResolveVariable(string identifier) =>
        throw new AssertException("Cannot add a variable into a global constructor scope");

    public ParameterInfo? GetParameterInfo(string name) => null;
    public ParameterDefinition ResolveParameter(string name) =>
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
}

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record GlobalConstructorScope(TranslationUnitContext Context) : IEmitScope, IDeclarationScope
{
    private MethodDefinition? _method;
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;

    public MethodDefinition Method => _method ??= Context.AssemblyContext.GetGlobalInitializer();
    public CTypeSystem CTypeSystem => Context.CTypeSystem;
    public bool TryGetFunctionInfo(string identifier, [NotNullWhen(true)] out FunctionInfo? functionInfo)
    {
        return Context.Functions.TryGetValue(identifier, out functionInfo);
    }
    public IReadOnlyDictionary<string, IType> GlobalFields => AssemblyContext.GlobalFields;

    public IReadOnlyDictionary<string, IType> Variables => ImmutableDictionary<string, IType>.Empty;
    public void AddVariable(string identifier, IType variable) =>
        throw new AssertException("Cannot add a variable into a global constructor scope");

    public bool TryGetVariable(string identifier, [NotNullWhen(true)] out IType? type)
    {
        type = null;
        return false;
    }
    public VariableDefinition ResolveVariable(string identifier) =>
        throw new AssertException("Cannot add a variable into a global constructor scope");

    public ParameterInfo? GetParameterInfo(string name) => null;
    public ParameterDefinition ResolveParameter(string name) =>
        throw new AssertException("Cannot resolve parameter from the global constructor scope");

    /// <inheritdoc />
    public IType ResolveType(IType type) => Context.ResolveType(type);
    public void AddTypeDefinition(string identifier, IType type) => Context.AddTypeDefinition(identifier, type);

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
    public void RegisterChildScope(IDeclarationScope childScope)
    {
        throw new AssertException("Cannot add child scope to a global constructor scope");
    }

    /// <inheritdoc />
    public string? GetBreakLabel()
    {
        return null;
    }
}

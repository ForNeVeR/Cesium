using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Cesium.CodeGen.Contexts;

internal record ForScope(IEmitScope Parent) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public CTypeSystem CTypeSystem => Parent.CTypeSystem;
    public bool TryGetFunctionInfo(string identifier, [NotNullWhen(true)] out FunctionInfo? functionInfo)
        => ((IDeclarationScope)Parent).TryGetFunctionInfo(identifier, out functionInfo);
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;
    private readonly Dictionary<string, IType> _variables = new();

    public bool TryGetVariable(string identifier, [NotNullWhen(true)] out IType? type)
    {
        var hasLocalDeclarations = _variables.TryGetValue(identifier, out type);
        if (hasLocalDeclarations)
        {
            Debug.Assert(type != null);
            return true;
        }

        return ((IDeclarationScope)Parent).TryGetVariable(identifier, out type);
    }
    public IReadOnlyDictionary<string, IType> GlobalFields => ((IDeclarationScope)Parent).GlobalFields;
    public void AddVariable(string identifier, IType variable) =>
        throw new WipException(205, "Variable addition into a for loop scope is not implemented, yet.");
    public VariableDefinition ResolveVariable(string identifier) => Parent.ResolveVariable(identifier); // no declarations for `for` now, so pass parent variables

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

    /// <inheritdoc />
    public void RegisterChildScope(IDeclarationScope childScope)
    {

    }

    private string _breakLabel = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string? GetBreakLabel()
    {
        return _breakLabel;
    }
}

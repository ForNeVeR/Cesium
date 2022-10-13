using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record SwitchScope(IEmitScope Parent) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public CTypeSystem CTypeSystem => Parent.CTypeSystem;
    public FunctionInfo? GetFunctionInfo(string identifier)
        => ((IDeclarationScope)Parent).GetFunctionInfo(identifier);
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;

    public IType? GetVariable(string identifier)
    {
        return ((IDeclarationScope)Parent).GetVariable(identifier);
    }
    public IReadOnlyDictionary<string, IType> GlobalFields => ((IDeclarationScope)Parent).GlobalFields;
    public void AddVariable(string identifier, IType variable) =>
        throw new WipException(205, "Variable addition into a switch scope is not implemented, yet.");
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

    private string _breakLabel = $"switch_{Guid.NewGuid()}";

    /// <inheritdoc />
    public string GetBreakLabel()
    {
        return _breakLabel;
    }
}

using System.Collections.Immutable;
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
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Context.Functions;
    public IReadOnlyDictionary<string, IType> GlobalFields => AssemblyContext.GlobalFields;

    public IReadOnlyDictionary<string, IType> Variables => ImmutableDictionary<string, IType>.Empty;
    public void AddVariable(string identifier, IType variable) =>
        throw new AssertException("Cannot add a variable into a global constructor scope");
    public VariableDefinition ResolveVariable(string identifier) =>
        throw new AssertException("Cannot add a variable into a global constructor scope");

    public ParameterInfo? GetParameterInfo(string name) => null;
    public ParameterDefinition ResolveParameter(string name) =>
        throw new AssertException("Cannot resolve parameter from the global constructor scope");
    public IType DisambiguateType(IType type) => Context.DisambiguateType(type);
    public void AddTypeDefinition(string identifier, IType type) => Context.AddTypeDefinition(identifier, type);
}

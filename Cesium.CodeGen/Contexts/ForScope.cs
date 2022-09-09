using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record ForScope(IEmitScope Parent) : IEmitScope, IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public CTypeSystem CTypeSystem => Parent.CTypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => ((IDeclarationScope)Parent).Functions;
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;
    public IReadOnlyDictionary<string, IType> Variables => ((IDeclarationScope)Parent).Variables; // no declarations for `for` now, so pass parent variables
    public IReadOnlyDictionary<string, IType> GlobalFields => ((IDeclarationScope)Parent).GlobalFields;
    public void AddVariable(string identifier, IType variable) =>
        throw new WipException(205, "Variable addition into a for loop scope is not implemented, yet.");
    public VariableDefinition ResolveVariable(string identifier) => Parent.ResolveVariable(identifier); // no declarations for `for` now, so pass parent variables

    public ParameterDefinition ResolveParameter(string name) => Parent.ResolveParameter(name);
    public ParameterInfo? GetParameterInfo(string name) => ((IDeclarationScope)Parent).GetParameterInfo(name);

    public Instruction? EndInstruction { get; set; }
}

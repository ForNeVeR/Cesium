using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record ForScope(IDeclarationScope Parent) : IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public TypeSystem TypeSystem => Parent.TypeSystem;
    public CTypeSystem CTypeSystem => Parent.CTypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Parent.Functions;
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;
    public IReadOnlyDictionary<string, IType> Variables => Parent.Variables; // no declarations for `for` now, so pass parent variables
    public void AddVariable(string identifier, IType variable) =>
        throw new WipException(205, "Variable addition into a for loop scope is not implemented, yet.");
    public VariableDefinition ResolveVariable(string identifier) => Parent.ResolveVariable(identifier); // no declarations for `for` now, so pass parent variables

    public ParameterDefinition? GetParameter(string name) => Parent.GetParameter(name);

    public Instruction? EndInstruction { get; set; }
}

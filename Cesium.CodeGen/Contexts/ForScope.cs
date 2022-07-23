using Cesium.CodeGen.Contexts.Meta;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record ForScope(IDeclarationScope Parent) : IDeclarationScope
{
    public AssemblyContext AssemblyContext => Parent.AssemblyContext;
    public ModuleDefinition Module => Parent.Module;
    public TypeSystem TypeSystem => Parent.TypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Parent.Functions;
    public TranslationUnitContext Context => Parent.Context;
    public MethodDefinition Method => Parent.Method;
    public Dictionary<string, VariableDefinition> Variables => Parent.Variables; // no declarations for `for` now, so pass parent variables
    public ParameterDefinition? GetParameter(string name) => Parent.GetParameter(name);

    public Instruction? EndInstruction { get; set; }
}
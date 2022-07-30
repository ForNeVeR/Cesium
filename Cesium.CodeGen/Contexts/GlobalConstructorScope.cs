using System.Collections.Immutable;
using Cesium.CodeGen.Contexts.Meta;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal record GlobalConstructorScope(TranslationUnitContext Context, MethodDefinition Method) : IDeclarationScope
{
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;
    public IReadOnlyDictionary<string, FunctionInfo> Functions => Context.Functions;

    public IReadOnlyDictionary<string, VariableDefinition> Variables => ImmutableDictionary<string, VariableDefinition>.Empty;
    public void AddVariable(string identifier, VariableDefinition variable) =>
        throw new NotSupportedException("Cannot add a variable into a global constructor scope");

    public ParameterDefinition? GetParameter(string name) => null;
}

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

public record FunctionScope(TranslationUnitContext Context, MethodDefinition Method)
{
    public AssemblyContext AssemblyContext => Context.AssemblyContext;
    public ModuleDefinition Module => Context.Module;
    public IReadOnlyDictionary<string, MethodReference> Functions => Context.Functions;

    public Dictionary<string, VariableDefinition> Variables { get; } = new();
}

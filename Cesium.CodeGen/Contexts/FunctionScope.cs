using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

public record FunctionScope(TranslationUnitContext Context, MethodDefinition Method)
{
    public ModuleDefinition Module => Context.Module;
    public IReadOnlyDictionary<string, MethodDefinition> Functions => Context.Functions;

    public Dictionary<string, VariableDefinition> Variables { get; } = new();
}

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen;

public record FunctionScope(ModuleDefinition Module, MethodDefinition Method)
{
    public Dictionary<string, VariableDefinition> Variables { get; } = new();
}

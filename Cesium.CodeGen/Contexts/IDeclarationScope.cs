using Cesium.CodeGen.Contexts.Meta;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal interface IDeclarationScope
{
    AssemblyContext AssemblyContext { get; }
    ModuleDefinition Module { get; }
    TypeSystem TypeSystem { get; }
    IReadOnlyDictionary<string, FunctionInfo> Functions { get; }
    TranslationUnitContext Context { get; }
    MethodDefinition Method { get; }
    Dictionary<string, VariableDefinition> Variables { get; }
}
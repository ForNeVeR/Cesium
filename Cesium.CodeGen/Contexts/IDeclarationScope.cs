using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal interface IDeclarationScope
{
    AssemblyContext AssemblyContext { get; }
    ModuleDefinition Module { get; }
    TypeSystem TypeSystem { get; }
    CTypeSystem CTypeSystem { get; }
    IReadOnlyDictionary<string, FunctionInfo> Functions { get; }
    TranslationUnitContext Context { get; }
    MethodDefinition Method { get; }
    IReadOnlyDictionary<string, IType> Variables { get; }
    void AddVariable(string identifier, IType variable);
    VariableDefinition ResolveVariable(string identifier);
    ParameterInfo? GetParameterInfo(string name);
    ParameterDefinition ResolveParameter(string name);
}

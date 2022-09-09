namespace Cesium.CodeGen.Contexts;

using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal interface IEmitScope
{
    CTypeSystem CTypeSystem { get; }
    MethodDefinition Method { get; }
    AssemblyContext AssemblyContext { get; }
    ModuleDefinition Module { get; }
    TranslationUnitContext Context { get; }
    VariableDefinition ResolveVariable(string identifier);
    ParameterDefinition ResolveParameter(string name);
}

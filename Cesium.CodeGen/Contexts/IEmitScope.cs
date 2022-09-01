namespace Cesium.CodeGen.Contexts;

using Mono.Cecil;
using Mono.Cecil.Cil;

internal interface IEmitScope : IDeclarationScope
{
    MethodDefinition Method { get; }
    AssemblyContext AssemblyContext { get; }
    ModuleDefinition Module { get; }
    TranslationUnitContext Context { get; }
    VariableDefinition ResolveVariable(string identifier);
    ParameterDefinition ResolveParameter(string name);
}

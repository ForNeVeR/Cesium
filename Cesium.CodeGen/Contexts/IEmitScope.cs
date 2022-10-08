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

    /// <summary>
    /// Resolves instruction to which label pointed.
    /// </summary>
    /// <param name="label">Label for which resolve </param>
    /// <returns>Instruction to which label pointed.</returns>
    Instruction ResolveLabel(string label);
}

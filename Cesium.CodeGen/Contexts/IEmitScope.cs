using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

internal interface IEmitScope
{
    MethodDefinition Method { get; }
    AssemblyContext AssemblyContext { get; }
    ModuleDefinition Module { get; }
    TranslationUnitContext Context { get; }
    VariableDefinition ResolveVariable(int varIndex);
    ParameterDefinition ResolveParameter(int index);

    /// <summary>
    /// Resolves instruction to which label pointed.
    /// </summary>
    /// <param name="label">Label for which resolve </param>
    /// <returns>Instruction to which label pointed.</returns>
    Instruction ResolveLabel(string label);

    public sealed FieldReference ResolveGlobalField(string name)
    {
        return Context.ResolveTranslationUnitField(name)
               ?? AssemblyContext.ResolveAssemblyLevelField(name, Context)
               ?? throw new CompilationException($"Global variable \"{name}\" not found.");
    }
}

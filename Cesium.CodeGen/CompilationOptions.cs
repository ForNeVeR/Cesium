using Mono.Cecil;

namespace Cesium.CodeGen;

public record CompilationOptions(
    TargetRuntimeDescriptor TargetRuntime,
    ModuleKind ModuleKind,
    string CorelibAssembly,
    string CesiumRuntime,
    IList<string> ImportAssemblies,
    string Namespace,
    string GlobalClassFqn);

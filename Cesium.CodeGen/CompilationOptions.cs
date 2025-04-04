// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Mono.Cecil;

namespace Cesium.CodeGen;

public record CompilationOptions(
    TargetRuntimeDescriptor TargetRuntime,
    TargetArchitectureSet TargetArchitectureSet,
    ModuleKind ModuleKind,
    string CorelibAssembly,
    string CesiumRuntime,
    IList<string> ImportAssemblies,
    string Namespace,
    string GlobalClassFqn,
    IList<string> DefineConstants,
    IList<string> AdditionalIncludeDirectories,
    bool ProducePreprocessedFile,
    bool ProduceAstFile);

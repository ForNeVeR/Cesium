// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Mono.Cecil;
using TruePath;

namespace Cesium.CodeGen;

public record CompilationOptions(
    TargetRuntimeDescriptor TargetRuntime,
    TargetArchitectureSet TargetArchitectureSet,
    ModuleKind ModuleKind,
    LocalPath CorelibAssembly,
    LocalPath CesiumRuntime,
    IList<LocalPath> ImportAssemblies,
    string Namespace,
    string GlobalClassFqn,
    IList<string> DefineConstants,
    IList<LocalPath> AdditionalIncludeDirectories,
    bool ProducePreprocessedFile,
    bool ProduceAstFile);

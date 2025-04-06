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
    bool ProduceAstFile)
{
    public virtual bool Equals(CompilationOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TargetRuntime.Equals(other.TargetRuntime)
               && TargetArchitectureSet == other.TargetArchitectureSet
               && ModuleKind == other.ModuleKind
               && CorelibAssembly.Equals(other.CorelibAssembly)
               && CesiumRuntime.Equals(other.CesiumRuntime)
               && ImportAssemblies.SequenceEqual(other.ImportAssemblies)
               && Namespace == other.Namespace
               && GlobalClassFqn == other.GlobalClassFqn
               && DefineConstants.SequenceEqual(other.DefineConstants)
               && AdditionalIncludeDirectories.SequenceEqual(other.AdditionalIncludeDirectories)
               && ProducePreprocessedFile == other.ProducePreprocessedFile
               && ProduceAstFile == other.ProduceAstFile;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(TargetRuntime);
        hashCode.Add(TargetArchitectureSet);
        hashCode.Add(ModuleKind);
        hashCode.Add(CorelibAssembly);
        hashCode.Add(CesiumRuntime);
        foreach (var importAssembly in ImportAssemblies)
        {
            hashCode.Add(importAssembly);
        }
        hashCode.Add(Namespace);
        hashCode.Add(GlobalClassFqn);
        foreach (var defineConstant in DefineConstants)
        {
            hashCode.Add(defineConstant);
        }
        foreach (var additionalIncludeDirectory in AdditionalIncludeDirectories)
        {
            hashCode.Add(additionalIncludeDirectory);
        }
        hashCode.Add(ProducePreprocessedFile);
        hashCode.Add(ProduceAstFile);
        return hashCode.ToHashCode();
    }
}

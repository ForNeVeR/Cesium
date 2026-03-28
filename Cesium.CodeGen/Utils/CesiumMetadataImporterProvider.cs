// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Utils;

/// <remarks>
/// <para>
///     The purpose of this code is to fix up any type references to System.Private.CoreLib and make sure they point
///     over to the correct reference assemblies instead.
/// </para>
/// <para>
///     The general idea taken from
///     <a href="https://github.com/adrianoc/cecilifier/blob/2ec64f40d508f5c64504913f8dd0f1ca84211973/Cecilifier.Runtime/TypeHelpers.cs#L92">Cecilifier</a>.
/// </para>
/// </remarks>
public class CesiumMetadataImporterProvider(TargetRuntimeDescriptor runtime) : IMetadataImporterProvider
{ // TODO[#970]: Review this mechanism after we start to manipulate with the actual reference assemblies.
  //             Note that we might not need this at all — just eliminate any mentions of
  //             Mono.Cecil.TypeSystem.CoreLibrary, and this whole type won't be necessary anymore.
    public IMetadataImporter GetMetadataImporter(ModuleDefinition module) =>
        new CesiumMetadataImporter(runtime, module);
}

public class CesiumMetadataImporter(TargetRuntimeDescriptor runtime, ModuleDefinition module) : DefaultMetadataImporter(module)
{
    protected override IMetadataScope ImportScope(TypeReference type)
    {
        if (type.Scope.Name == "System.Private.CoreLib.dll")
        {
            var reference = type.FullName switch
            {
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute" => runtime.GetSystemAssemblyReference(),
                "System.Runtime.CompilerServices.FixedBufferAttribute" => runtime.GetSystemAssemblyReference(),
                "System.Runtime.CompilerServices.UnsafeValueTypeAttribute" => runtime.GetSystemAssemblyReference(),
                "System.Type" => runtime.GetSystemAssemblyReference(),
                "System.ValueType" => runtime.GetSystemAssemblyReference(),
                _ => throw new AssertException(
                    $"I don't know what system assembly to use instead of System.Private.CoreLib " +
                    $"to import type {type.FullName}.")
            };
            if (!module.AssemblyReferences.Contains(reference))
                module.AssemblyReferences.Add(reference);
            return reference;
        }

        return base.ImportScope(type);
    }
}

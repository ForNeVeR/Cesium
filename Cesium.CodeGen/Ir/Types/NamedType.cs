using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record NamedType(string TypeName) : IType
{
    /// <inheritdoc />
    public TypeKind TypeKind => TypeKind.Unresolved;

    public TypeReference Resolve(TranslationUnitContext context) =>
        throw new AssertException($"Type {TypeName} was never resolved.");

    public int? GetSizeInBytes(TargetArchitectureSet arch) =>
        throw new AssertException($"Type {TypeName} was never resolved.");
}

using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

public record NamedType(string TypeName) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) =>
        throw new AssertException($"Type {TypeName} was never resolved.");

    public int SizeInBytes =>
        throw new AssertException($"Type {TypeName} was never resolved.");

    // explicit impl while Size not implemented
    public override string ToString()
        => $"NamedType {{ TypeName = {TypeName} }}";
}

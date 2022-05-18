using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

public record NamedType(string TypeName) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) =>
        context.GetTypeReference(TypeName) ?? throw new NotSupportedException($"Type not found: {TypeName}.");

    public int SizeInBytes => throw new NotImplementedException($"Could not calculate size for {TypeName} yet.");

    // explicit impl while Size not implemented
    public override string ToString()
        => $"PointerType {{ TypeName = {TypeName} }}";
}

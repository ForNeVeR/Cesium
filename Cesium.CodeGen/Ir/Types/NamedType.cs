using Cesium.CodeGen.Contexts;
using Cesium.Core.Exceptions;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

public record NamedType(string TypeName) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) =>
        context.GetTypeReference(TypeName) ?? throw new CompilationException($"Type not found: {TypeName}.");

    public int SizeInBytes => throw new WipException(232, $"Could not calculate size for {TypeName} yet.");

    // explicit impl while Size not implemented
    public override string ToString()
        => $"NamedType {{ TypeName = {TypeName} }}";
}

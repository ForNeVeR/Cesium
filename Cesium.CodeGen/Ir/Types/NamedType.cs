using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

public record NamedType(string TypeName) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) =>
        context.GetTypeReference(TypeName) ?? throw new NotSupportedException($"Type not found: {TypeName}.");
}

using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record ResolvedPointerType(TypeReference ResolvedType) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) => ResolvedType;
    public int? GetSizeInBytes(TargetArchitectureSet arch) => PointerType.SizeInBytes(arch);
}

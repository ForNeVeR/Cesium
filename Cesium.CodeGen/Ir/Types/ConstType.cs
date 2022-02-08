using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record ConstType(IType Base) : IType
{
    public TypeReference Resolve(TypeSystem typeSystem) => Base.Resolve(typeSystem);
}

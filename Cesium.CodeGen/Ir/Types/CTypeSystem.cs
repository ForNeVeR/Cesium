using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal class CTypeSystem
{
    public IType Boolean { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType Char { get; } = new PrimitiveType(PrimitiveTypeKind.Char);
    public IType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType CharPtr { get; } = new PointerType(new PrimitiveType(PrimitiveTypeKind.Char));
}

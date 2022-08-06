using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.Types;

internal class CTypeSystem
{
    public IType Boolean { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType Char { get; } = new PrimitiveType(PrimitiveTypeKind.Char);
    public IType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType CharPtr { get; } = new PrimitiveType(PrimitiveTypeKind.Char).MakePointerType();
}

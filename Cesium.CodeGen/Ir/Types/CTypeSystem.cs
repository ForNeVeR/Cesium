using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.Types;

internal class CTypeSystem
{
    public IType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Int); // TODO[#179]: Figure out the right type.
    public IType Char { get; } = new PrimitiveType(PrimitiveTypeKind.Char);
    public IType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType CharPtr { get; } = new PrimitiveType(PrimitiveTypeKind.Char).MakePointerType();
    public IType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);
    public IType Double { get; } = new PrimitiveType(PrimitiveTypeKind.Double);
}

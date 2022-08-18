using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.Types;

internal class CTypeSystem
{
    public IType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Int); // TODO[#179]: Figure out the right type.
    public IType Char { get; } = new PrimitiveType(PrimitiveTypeKind.Char);
    public IType SignedChar { get; } = new PrimitiveType(PrimitiveTypeKind.SignedChar);
    public IType Short { get; } = new PrimitiveType(PrimitiveTypeKind.Short);
    public IType UnsignedShort { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedShort);
    public IType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType UnsignedInt { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedInt);
    public IType Long { get; } = new PrimitiveType(PrimitiveTypeKind.Long);
    public IType UnsignedLong { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedLong);
    public IType CharPtr { get; } = new PrimitiveType(PrimitiveTypeKind.Char).MakePointerType();
    public IType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);
    public IType Double { get; } = new PrimitiveType(PrimitiveTypeKind.Double);
}

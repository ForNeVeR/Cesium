using Cesium.CodeGen.Extensions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Types;

internal class CTypeSystem
{
    public static IType Void { get; } = new PrimitiveType(PrimitiveTypeKind.Void);
    public static IType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Int); // TODO[#179]: Figure out the right type.
    public static IType Char { get; } = new PrimitiveType(PrimitiveTypeKind.Char);
    public static IType SignedChar { get; } = new PrimitiveType(PrimitiveTypeKind.SignedChar);
    public static IType Short { get; } = new PrimitiveType(PrimitiveTypeKind.Short);
    public static IType UnsignedShort { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedShort);
    public static IType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public static IType UnsignedInt { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedInt);
    public static IType Long { get; } = new PrimitiveType(PrimitiveTypeKind.Long);
    public static IType UnsignedLong { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedLong);
    public static IType CharPtr { get; } = new PrimitiveType(PrimitiveTypeKind.Char).MakePointerType();
    public static IType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);
    public static IType Double { get; } = new PrimitiveType(PrimitiveTypeKind.Double);
    public static IType NativeInt { get; } = new PrimitiveType(PrimitiveTypeKind.NativeInt);
    public static IType NativeUInt { get; } = new PrimitiveType(PrimitiveTypeKind.NativeUInt);

    public static bool IsConversionAvailable(IType type, IType targetType)
    {
        if (type.IsEqualTo(targetType)
            || (type.IsBool() && targetType.IsInteger())
            || (targetType.IsBool() && type.IsInteger()))
            return true;

        if (!type.IsNumeric())
            return false;

        if (targetType.Equals(SignedChar))
            return true;
        else if (targetType.Equals(Short))
            return true;
        else if (targetType.Equals(Int))
            return true;
        else if (targetType.Equals(Long))
            return true;
        else if (targetType.Equals(Char))
            return true;
        else if (targetType.Equals(UnsignedShort))
            return true;
        else if (targetType.Equals(UnsignedInt))
            return true;
        else if (targetType.Equals(UnsignedLong))
            return true;
        else if (targetType.Equals(Float))
            return true;
        else if (targetType.Equals(Double))
            return true;
        else
            return false;
    }
}

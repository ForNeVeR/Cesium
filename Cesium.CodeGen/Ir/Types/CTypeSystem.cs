using Cesium.CodeGen.Extensions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Types;

internal class CTypeSystem
{
    public IType Void { get; } = new PrimitiveType(PrimitiveTypeKind.Void);
    public IType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Int); // TODO[#179]: Figure out the right type.
    public IType Char { get; } = new PrimitiveType(PrimitiveTypeKind.Char);
    public IType SignedChar { get; } = new PrimitiveType(PrimitiveTypeKind.SignedChar);
    public IType UnsignedChar { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedChar);
    public IType Short { get; } = new PrimitiveType(PrimitiveTypeKind.Short);
    public IType UnsignedShort { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedShort);
    public IType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);
    public IType UnsignedInt { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedInt);
    public IType Long { get; } = new PrimitiveType(PrimitiveTypeKind.Long);
    public IType UnsignedLong { get; } = new PrimitiveType(PrimitiveTypeKind.UnsignedLong);
    public IType CharPtr { get; } = new PrimitiveType(PrimitiveTypeKind.Char).MakePointerType();
    public IType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);
    public IType Double { get; } = new PrimitiveType(PrimitiveTypeKind.Double);
    public IType NativeInt { get; } = new PrimitiveType(PrimitiveTypeKind.NativeInt);
    public IType NativeUInt { get; } = new PrimitiveType(PrimitiveTypeKind.NativeUInt);

    public bool IsConversionAvailable(IType type, IType targetType)
    {
        if (type.IsEqualTo(targetType)
            || (this.IsBool(type) && this.IsInteger(targetType))
            || (this.IsBool(targetType) && this.IsInteger(type)))
            return true;

        if (!this.IsNumeric(type))
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

    internal bool IsConversionRequired(IType type, IType targetType)
    {
        if (type.IsEqualTo(targetType))
            return false;

        if (!this.IsNumeric(type))
            throw new CompilationException($"Conversion from {type} to {targetType} is not supported.");

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
        else if (targetType.Equals(NativeInt) || targetType is PointerType)
            return true;
        else
            throw new CompilationException($"Conversion from {type} to {targetType} is not supported.");
    }
}

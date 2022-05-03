using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal enum PrimitiveTypeKind
{
    // Basic
    Void,
    Char,
    Short,
    Signed,
    Int,
    Unsigned,
    Long,
    Float,
    Double,

    // Unsigned
    UnsignedChar,
    UnsignedShort,
    UnsignedShortInt,
    UnsignedInt,
    UnsignedLong,
    UnsignedLongInt,
    UnsignedLongLong,
    UnsignedLongLongInt,

    // Signed
    SignedChar,
    SignedShort,
    ShortInt,
    SignedShortInt,
    SignedInt,
    SignedLong,
    LongInt,
    SignedLongInt,
    LongLong,
    SignedLongLong,
    LongLongInt,
    SignedLongLongInt,
    LongDouble,
}

internal record PrimitiveType(PrimitiveTypeKind Kind) : IType
{
    public TypeReference Resolve(TranslationUnitContext context)
    {
        var typeSystem = context.TypeSystem;
        return Kind switch
        {
            // Basic
            PrimitiveTypeKind.Void => typeSystem.Void,
            PrimitiveTypeKind.Char => typeSystem.Byte,
            PrimitiveTypeKind.Short => typeSystem.SByte,
            PrimitiveTypeKind.Signed => typeSystem.Int16,
            PrimitiveTypeKind.Int => typeSystem.Int32,
            PrimitiveTypeKind.Unsigned => typeSystem.UInt32,
            PrimitiveTypeKind.Long => typeSystem.Int64,
            PrimitiveTypeKind.Float => typeSystem.Single,
            PrimitiveTypeKind.Double => typeSystem.Double,

            // Unsigned
            PrimitiveTypeKind.UnsignedChar => typeSystem.Byte,
            PrimitiveTypeKind.UnsignedShort => typeSystem.UInt16,
            PrimitiveTypeKind.UnsignedShortInt => typeSystem.UInt16,
            PrimitiveTypeKind.UnsignedInt => typeSystem.UInt32,
            PrimitiveTypeKind.UnsignedLong => typeSystem.UInt64,
            PrimitiveTypeKind.UnsignedLongInt => typeSystem.UInt64,
            PrimitiveTypeKind.UnsignedLongLong => typeSystem.UInt64,
            PrimitiveTypeKind.UnsignedLongLongInt => typeSystem.UInt64,

            // Signed
            PrimitiveTypeKind.SignedChar => typeSystem.SByte,
            PrimitiveTypeKind.SignedShort => typeSystem.Int16,
            PrimitiveTypeKind.ShortInt => typeSystem.Int16,
            PrimitiveTypeKind.SignedShortInt => typeSystem.Int16,
            PrimitiveTypeKind.SignedInt => typeSystem.Int32,
            PrimitiveTypeKind.SignedLong => typeSystem.Int64,
            PrimitiveTypeKind.LongInt => typeSystem.Int64,
            PrimitiveTypeKind.SignedLongInt => typeSystem.Int64,
            PrimitiveTypeKind.LongLong => typeSystem.Int64,
            PrimitiveTypeKind.SignedLongLong => typeSystem.Int64,
            PrimitiveTypeKind.LongLongInt => typeSystem.Int64,
            PrimitiveTypeKind.SignedLongLongInt => typeSystem.Int64,
            PrimitiveTypeKind.LongDouble => typeSystem.Double,

            _ => throw new NotImplementedException($"Primitive type not supported, yet: {this}.")
        };
    }
}

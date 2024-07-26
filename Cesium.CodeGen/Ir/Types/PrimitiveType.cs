using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Types;

internal enum PrimitiveTypeKind
{
    // Basic
    Void,
    Char,
    Short,
    Signed,
    Int,
    Long,
    Float,
    Double,

    // Unsigned
    UnsignedChar,
    UnsignedShort,
    UnsignedShortInt,
    Unsigned,
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

    // This is band-aid type until we would be able perform lowering in controlled fashion.
    NativeInt,
    NativeUInt,
    Bool,
}

internal record PrimitiveType(PrimitiveTypeKind Kind) : IType
{
    /// <inheritdoc />
    public TypeKind TypeKind => TypeKind.PrimitiveType;

    public TypeReference Resolve(TranslationUnitContext context)
    {
        var typeSystem = context.TypeSystem;
        return Kind switch
        {
            // Basic
            PrimitiveTypeKind.Void => typeSystem.Void,
            PrimitiveTypeKind.Char => typeSystem.Byte,
            PrimitiveTypeKind.Short => typeSystem.Int16,
            PrimitiveTypeKind.Signed => typeSystem.Int32,
            PrimitiveTypeKind.Int => typeSystem.Int32,
            PrimitiveTypeKind.Long => typeSystem.Int64,
            PrimitiveTypeKind.Float => typeSystem.Single,
            PrimitiveTypeKind.Double => typeSystem.Double,

            // Unsigned
            PrimitiveTypeKind.UnsignedChar => typeSystem.Byte,
            PrimitiveTypeKind.UnsignedShort => typeSystem.UInt16,
            PrimitiveTypeKind.UnsignedShortInt => typeSystem.UInt16,
            PrimitiveTypeKind.Unsigned => typeSystem.UInt32,
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

            PrimitiveTypeKind.NativeUInt => typeSystem.UIntPtr,

            PrimitiveTypeKind.Bool => typeSystem.Boolean,

            _ => throw new AssertException($"Primitive type not supported: {Kind}.")
        };
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch) =>
        Kind switch
        {
            // Basic
            PrimitiveTypeKind.Char => 1,
            PrimitiveTypeKind.Short => 2,
            PrimitiveTypeKind.Signed => 4,
            PrimitiveTypeKind.Int => 4,
            PrimitiveTypeKind.Long => 8,
            PrimitiveTypeKind.Float => 4,
            PrimitiveTypeKind.Double => 8,

            // Unsigned
            PrimitiveTypeKind.UnsignedChar => 1,
            PrimitiveTypeKind.UnsignedShort => 2,
            PrimitiveTypeKind.UnsignedShortInt => 2,
            PrimitiveTypeKind.Unsigned => 4,
            PrimitiveTypeKind.UnsignedInt => 4,
            PrimitiveTypeKind.UnsignedLong => 8,
            PrimitiveTypeKind.UnsignedLongInt => 8,
            PrimitiveTypeKind.UnsignedLongLong => 8,
            PrimitiveTypeKind.UnsignedLongLongInt => 8,

            // Signed
            PrimitiveTypeKind.SignedChar => 1,
            PrimitiveTypeKind.SignedShort => 2,
            PrimitiveTypeKind.ShortInt => 2,
            PrimitiveTypeKind.SignedShortInt => 2,
            PrimitiveTypeKind.SignedInt => 4,
            PrimitiveTypeKind.SignedLong => 8,
            PrimitiveTypeKind.LongInt => 8,
            PrimitiveTypeKind.SignedLongInt => 8,
            PrimitiveTypeKind.LongLong => 8,
            PrimitiveTypeKind.SignedLongLong => 8,
            PrimitiveTypeKind.LongLongInt => 8,
            PrimitiveTypeKind.SignedLongLongInt => 8,
            PrimitiveTypeKind.LongDouble => 8,

            PrimitiveTypeKind.NativeInt => arch switch
            {
                TargetArchitectureSet.Dynamic => null,
                TargetArchitectureSet.Bit32 => 4,
                TargetArchitectureSet.Bit64 => 8,
                _ => throw new AssertException($"Architecture set not supported: {arch}.")
            },
            PrimitiveTypeKind.NativeUInt => arch switch
            {
                TargetArchitectureSet.Dynamic => null,
                TargetArchitectureSet.Bit32 => 4,
                TargetArchitectureSet.Bit64 => 8,
                _ => throw new AssertException($"Architecture set not supported: {arch}.")
            },
            PrimitiveTypeKind.Void => 1,
            PrimitiveTypeKind.Bool => 1,

            _ => throw new AssertException($"Could not calculate size for {Kind}."),
        };

    public override string ToString()
        => $"PrimitiveType {{ Kind = {Kind} }}";
}

internal static class PrimitiveTypeInfo
{
    internal static readonly Dictionary<PrimitiveTypeKind, int> Size = new()
    {
        { PrimitiveTypeKind.Char, 1 },
        { PrimitiveTypeKind.SignedChar, 1 },
        { PrimitiveTypeKind.Short, 2 },
        { PrimitiveTypeKind.UnsignedShort, 2 },
        { PrimitiveTypeKind.Int, 4 },
        { PrimitiveTypeKind.UnsignedInt, 4 },
        { PrimitiveTypeKind.Float, 4 },
        { PrimitiveTypeKind.Long, 8 },
        { PrimitiveTypeKind.UnsignedLong, 8 },
        { PrimitiveTypeKind.Double, 8 },
        { PrimitiveTypeKind.Bool, 1 },
    };

    internal static readonly Dictionary<PrimitiveTypeKind, (OpCode load, OpCode store)> Opcodes = new()
    {
        { PrimitiveTypeKind.Char, (OpCodes.Ldind_I1, OpCodes.Stind_I1) },
        { PrimitiveTypeKind.SignedChar, (OpCodes.Ldind_I1, OpCodes.Stind_I1) },
        { PrimitiveTypeKind.UnsignedChar, (OpCodes.Ldind_U1, OpCodes.Stind_I1) },
        { PrimitiveTypeKind.Short, (OpCodes.Ldind_I2, OpCodes.Stind_I2) },
        { PrimitiveTypeKind.UnsignedShort, (OpCodes.Ldind_U2, OpCodes.Stind_I2) },
        { PrimitiveTypeKind.Int, (OpCodes.Ldind_I4, OpCodes.Stind_I4) },
        { PrimitiveTypeKind.UnsignedInt, (OpCodes.Ldind_U4, OpCodes.Stind_I4) },
        { PrimitiveTypeKind.Float, (OpCodes.Ldind_R4, OpCodes.Stind_R4) },
        { PrimitiveTypeKind.Long, (OpCodes.Ldind_I8, OpCodes.Stind_I8) },
        { PrimitiveTypeKind.UnsignedLong, (OpCodes.Ldind_I8, OpCodes.Stind_I8) },
        { PrimitiveTypeKind.Double, (OpCodes.Ldind_R8, OpCodes.Stind_R8) },
        { PrimitiveTypeKind.Bool, (OpCodes.Ldind_I1, OpCodes.Stind_I1) },
    };
}

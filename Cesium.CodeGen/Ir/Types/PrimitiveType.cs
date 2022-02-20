using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal enum PrimitiveTypeKind
{
    // Basic
    Char,
    Int,
    Void,

    // Unsigned
    UnsignedChar
}

internal record PrimitiveType(PrimitiveTypeKind Kind) : IType
{
    public TypeReference Resolve(TranslationUnitContext context)
    {
        var typeSystem = context.TypeSystem;
        return Kind switch
        {
            // Basic
            PrimitiveTypeKind.Char => typeSystem.Byte,
            PrimitiveTypeKind.Int => typeSystem.Int32,
            PrimitiveTypeKind.Void => typeSystem.Void,

            // Unsigned
            PrimitiveTypeKind.UnsignedChar => typeSystem.Byte,

            _ => throw new NotImplementedException($"Primitive type not supported, yet: {this}.")
        };
    }
}

using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal enum PrimitiveTypeKind
{
    Char,
    Int,
    Void
}

internal record PrimitiveType(PrimitiveTypeKind Kind) : IType
{
    public TypeReference Resolve(TypeSystem typeSystem) => Kind switch
    {
        PrimitiveTypeKind.Char => typeSystem.Byte,
        PrimitiveTypeKind.Int => typeSystem.Int32,
        PrimitiveTypeKind.Void => typeSystem.Void,
        _ => throw new NotImplementedException($"Primitive type not supported, yet: {this}.")
    };
}

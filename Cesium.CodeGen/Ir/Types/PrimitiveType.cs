using Cesium.CodeGen.Contexts;
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
    public TypeReference Resolve(TranslationUnitContext context)
    {
        var typeSystem = context.TypeSystem;
        return Kind switch
        {
            PrimitiveTypeKind.Char => typeSystem.Byte,
            PrimitiveTypeKind.Int => typeSystem.Int32,
            PrimitiveTypeKind.Void => typeSystem.Void,
            _ => throw new NotImplementedException($"Primitive type not supported, yet: {this}.")
        };
    }
}

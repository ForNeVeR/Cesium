using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

public static class TypeSpecifierEx
{
    public static TypeReference GetTypeReference(this TypeSpecifier specifier, TypeSystem typeSystem) =>
        specifier.TypeName switch
        {
            "char" => typeSystem.Byte,
            "int" => typeSystem.Int32,
            "void" => typeSystem.Void,
            var unknown => throw new Exception($"Unknown type specifier: {unknown}.")
        };
}

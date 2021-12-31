using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

public static class TypeSpecifierEx
{
    public static TypeReference GetTypeReference(this TypeSpecifier specifier, ModuleDefinition module) =>
        specifier.TypeName switch
        {
            "char" => module.TypeSystem.Byte,
            "int" => module.TypeSystem.Int32,
            var unknown => throw new Exception($"Unknown type specifier: {unknown}")
        };
}

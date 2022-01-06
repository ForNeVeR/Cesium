using Cesium.Ast;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Extensions;

public static class DeclarationSpecifierEx
{
    public static TypeReference GetTypeReference(
        this IEnumerable<IDeclarationSpecifier> specifiers,
        Declarator? declarator,
        TypeSystem typeSystem)
    {
        TypeReference? type = null;
        foreach (var specifier in specifiers)
        {
            type = specifier switch
            {
                TypeSpecifier ts when type == null => ts.TypeName switch
                {
                    "char" => typeSystem.Byte,
                    "int" => typeSystem.Int32,
                    "void" => typeSystem.Void,
                    var unknown => throw new Exception($"Unknown type specifier: {unknown}.")
                },
                TypeSpecifier => throw new NotSupportedException(
                    "Multiple type specifiers for declaration are not supported."),
                TypeQualifier { Name: "const" } => type, // TODO: process const declarations; ignored for now
                _ => throw new NotImplementedException($"Declaration specifier {specifier} isn't supported, yet.")
            };
        }

        if (type == null)
            throw new NotSupportedException("Type wasn't determined from the declaration specifiers.");

        if (declarator == null)
            return type;

        var (pointer, directDeclarator) = declarator;
        return type.Apply(pointer).Apply(directDeclarator);
    }

    private static TypeReference Apply(this TypeReference type, Pointer? pointer) => pointer switch
    {
        null => type,
        _ when pointer == new Pointer() => type.MakePointerType(),
        _ => throw new NotImplementedException($"Complex pointer type not supported, yet: {pointer}.")
    };

    private static TypeReference Apply(this TypeReference type, IDirectDeclarator declarator)
    {
        type = declarator switch
        {
            ArrayDirectDeclarator { TypeQualifiers: null, Size: null } => type.MakePointerType(),
            IdentifierDirectDeclarator or IdentifierListDirectDeclarator or ParameterListDirectDeclarator => type,
            _ => throw new NotImplementedException($"Declarator {declarator} isn't supported, yet")
        };

        return declarator.Base == null ? type : type.Apply(declarator.Base);
    }
}

using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class ParameterDeclarationEx
{
    public static TypeReference GetTypeReference(this ParameterDeclaration parameter, TypeSystem typeSystem)
    {
        // Verify that the declarator only has name defined and nothing else:
        if (parameter.Declarator is not
            { Pointer: null, DirectDeclarator: IdentifierDirectDeclarator })
            throw new NotImplementedException($"Parameter declarator shape not supported: {parameter}.");

        var typeSpecifier = (TypeSpecifier)parameter.Specifiers.Single();
        return typeSpecifier.GetTypeReference(typeSystem);
    }
}

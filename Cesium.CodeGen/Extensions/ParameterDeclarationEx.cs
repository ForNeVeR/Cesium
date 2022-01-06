using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class ParameterDeclarationEx
{
    public static TypeReference GetTypeReference(this ParameterDeclaration parameter, TypeSystem typeSystem) =>
        parameter.Specifiers.GetTypeReference(parameter.Declarator, typeSystem);
}

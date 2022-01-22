using Cesium.Ast;
using Cesium.CodeGen.Ir;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class ParameterDeclarationEx
{
    public static ParameterDefinition CreateParameterDefinition(
        this ParameterDeclaration parameter,
        TypeSystem typeSystem)
    {
        var type = parameter.GetTypeReference(typeSystem);

        var declarator = parameter.Declarator;
        if (declarator != null)
            return new ParameterDefinition(type) { Name = declarator.DirectDeclarator.GetIdentifier() };

        if (type.Equals(typeSystem.Void))
            return new ParameterDefinition(type); // a marker for parameterless function

        var abstractDeclarator = parameter.AbstractDeclarator;
        if (abstractDeclarator == null)
            throw new NotSupportedException(
                $"Parameter where neither declarator nor abstract declarator is present: {parameter}.");

        throw new NotSupportedException($"Parameters without names aren't supported, yet: {parameter}.");
    }

    private static TypeReference GetTypeReference(this ParameterDeclaration parameter, TypeSystem typeSystem) =>
        DeclarationInfo.Of(parameter.Specifiers, parameter.Declarator!.DirectDeclarator).Type.Resolve(typeSystem);
}

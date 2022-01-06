using Cesium.Ast;

namespace Cesium.CodeGen.Extensions;

public static class DirectDeclaratorEx
{
    public static string GetIdentifier(this IDirectDeclarator declarator) => declarator switch
    {
        IdentifierDirectDeclarator id => id.Identifier,
        _ => throw new ArgumentException(
            $"Attempt to extract an identifier from a declarator of incorrect shape: {declarator}.")
    };

    public static ParameterTypeList GetParameterTypeList(this IDirectDeclarator declarator) => declarator switch
    {
        ParameterListDirectDeclarator pl => pl.Parameters,
        _ => throw new ArgumentException(
            $"Attempt to extract a parameter type list from a declarator of incorrect shape: {declarator}.")
    };
}

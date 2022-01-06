using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class FunctionDefinitionEx
{
    public static TypeReference GetReturnType(this FunctionDefinition function, TypeSystem typeSystem)
    {
        var typeSpecifier = function.Specifiers.OfType<TypeSpecifier>().Single();
        return typeSpecifier.GetTypeReference(typeSystem);
    }

    public static IEnumerable<TypeReference> GetParameterTypes(this FunctionDefinition function, TypeSystem typeSystem)
    {
        var declarator = function.Declarator;
        if (declarator.Pointer != null)
            throw new NotImplementedException("Unsupported construction: function declarator with pointer.");

        var parameters = declarator.DirectDeclarator.ParameterList;
        if (parameters == null)
        {
            // Empty parameter list; let's consider it the same as void for now.
            return Enumerable.Empty<TypeReference>();
        }

        if (parameters.IsVararg)
            throw new NotImplementedException("Vararg parameters aren't supported, yet.");

        return parameters.Parameters.Select(p => p.GetTypeReference(typeSystem));
    }
}

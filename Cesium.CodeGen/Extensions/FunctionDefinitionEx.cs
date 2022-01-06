using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class FunctionDefinitionEx
{
    public static TypeReference GetReturnType(this FunctionDefinition function, TypeSystem typeSystem) =>
        function.Specifiers.GetTypeReference(function.Declarator, typeSystem);

    public static IEnumerable<TypeReference> GetParameterTypes(this FunctionDefinition function, TypeSystem typeSystem)
    {
        var declarator = function.Declarator;
        if (declarator.Pointer != null)
            throw new NotImplementedException("Unsupported construction: function declarator with pointer.");

        var parameters = declarator.DirectDeclarator.GetParameterTypeList();
        if (parameters == null)
        {
            // Empty parameter list; let's consider it the same as void for now.
            return Enumerable.Empty<TypeReference>();
        }

        if (parameters.IsVararg)
            throw new NotImplementedException("Vararg parameters aren't supported, yet.");

        var types = parameters.Parameters.Select(p => p.GetTypeReference(typeSystem)).ToList();
        if (types.Count == 1 && types[0].Equals(typeSystem.Void))
            return Enumerable.Empty<TypeReference>();

        return types;
    }
}

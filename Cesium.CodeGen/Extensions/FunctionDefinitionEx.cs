using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class FunctionDefinitionEx
{
    public static TypeReference GetReturnType(this FunctionDefinition function, TypeSystem typeSystem) =>
        function.Specifiers.GetTypeReference(function.Declarator, typeSystem);

    public static IEnumerable<ParameterDefinition> GetParameters(this FunctionDefinition function, TypeSystem typeSystem)
    {
        var declarator = function.Declarator;
        if (declarator.Pointer != null)
            throw new NotImplementedException("Unsupported construction: function declarator with pointer.");

        var parameters = declarator.DirectDeclarator.GetParameterTypeList();
        if (parameters == null)
        {
            // Empty parameter list; let's consider it the same as void for now.
            return Enumerable.Empty<ParameterDefinition>();
        }

        if (parameters.IsVararg)
            throw new NotImplementedException("Vararg parameters aren't supported, yet.");

        var definitions = parameters.Parameters.Select(p => p.CreateParameterDefinition(typeSystem)).ToList();
        if (definitions.Count == 1 && definitions[0].ParameterType.Equals(typeSystem.Void))
            return Enumerable.Empty<ParameterDefinition>();

        return definitions;
    }
}

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class TypeDefinitionEx
{
    public static MethodDefinition DefineMethod(
        this TypeDefinition type,
        TranslationUnitContext context,
        string name,
        TypeReference returnType,
        ParametersInfo? parameters)
    {
        var method = new MethodDefinition(
            name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType);

        var _ = method.Body.Instructions; // Initialize instructions with empty collection to avoid NRE
        AddParameters(context, method, parameters);
        type.Methods.Add(method);
        return method;
    }

    private static void AddParameters(
        TranslationUnitContext context,
        MethodReference method,
        ParametersInfo? parametersInfo)
    {
        if (parametersInfo == null) return;
        var (parameters, isVoid, isVarArg) = parametersInfo;
        if (isVoid) return;
        if (isVarArg)
            throw new NotImplementedException($"VarArg functions not supported, yet: {method.Name}.");

        // TODO[#87]: Process empty (non-void) parameter list.

        foreach (var parameter in parameters)
        {
            var (type, name) = parameter;
            var parameterDefinition = new ParameterDefinition(type.Resolve(context))
            {
                Name = name
            };
            method.Parameters.Add(parameterDefinition);
        }
    }
}

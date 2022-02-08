using Cesium.CodeGen.Ir;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class TypeDefinitionEx
{
    public static MethodDefinition DefineMethod(
        this TypeDefinition type,
        TypeSystem typeSystem,
        string name,
        TypeReference returnType,
        ParametersInfo? parameters)
    {
        var method = new MethodDefinition(
            name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType);
        AddParameters(typeSystem, method, parameters);
        type.Methods.Add(method);
        return method;
    }

    private static void AddParameters(TypeSystem typeSystem, MethodDefinition method, ParametersInfo? parametersInfo)
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
            var parameterDefinition = new ParameterDefinition(type.Resolve(typeSystem))
            {
                Name = name
            };
            method.Parameters.Add(parameterDefinition);
        }
    }
}

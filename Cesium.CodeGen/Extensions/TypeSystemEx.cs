using System.Reflection;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class TypeSystemEx
{
    public static MethodReference? MethodLookup(this TranslationUnitContext context, string memberName, ParametersInfo? parametersInfo = null)
    {
        var components = memberName.Split("::", 2);
        if (components.Length != 2)
            throw new NotSupportedException($"Invalid CLI member name: {memberName}.");

        var typeName = components[0];
        var methodName = components[1];

        Type[]? types = parametersInfo?.Parameters.Select(x => x.Type.Resolve(context).GetTypeObject()).OfType<Type>().ToArray();

        if (types?.Length != parametersInfo?.Parameters.Count)
            return null;

        var method = FindMethod(context.AssemblyContext.ImportAssemblies, typeName, methodName, types);
        return method == null ? null : context.Module.ImportReference(method);
    }

    private static MethodInfo? FindMethod(IEnumerable<Assembly> assemblies, string typeName, string methodName, Type[]? parametersType = null)
    {
        foreach (var assembly in assemblies)
        {
            var method = FindMethod(assembly, typeName, methodName,parametersType );
            if (method != null)
                return method;
        }

        return null;
    }

    private static MethodInfo? FindMethod(Assembly assembly, string typeName, string methodName, Type[]? parametersType = null)
    {
        var type = assembly.GetType(typeName);

        var method = parametersType is not null ? type?.GetMethod(methodName, parametersType)
                                                : type?.GetMethod(methodName);
        return method;
    }

    public static Type? GetTypeObject(this TypeReference typeReference)
    {
        return Type.GetType($"{typeReference.FullName},{typeReference.Scope}");
    }

    public static bool IsEqual(this TypeReference typeReferenceA, TypeReference typeReferenceB)
    {
        return typeReferenceA.FullName == typeReferenceB.FullName
            && typeReferenceA.Scope == typeReferenceB.Scope;
    }
}

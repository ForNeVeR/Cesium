using System.Reflection;
using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class TypeSystemEx
{
    public static MethodReference? MethodLookup(this TranslationUnitContext context, string memberName)
    {
        var components = memberName.Split("::", 2);
        if (components.Length != 2)
            throw new NotSupportedException($"Invalid CLI member name: {memberName}.");

        var typeName = components[0];
        var methodName = components[1];

        var method = FindMethod(context.AssemblyContext.ImportAssemblies, typeName, methodName);
        return method == null ? null : context.Module.ImportReference(method);
    }

    private static MethodInfo? FindMethod(IEnumerable<Assembly> assemblies, string typeName, string methodName)
    {
        foreach (var assembly in assemblies)
        {
            var method = FindMethod(assembly, typeName, methodName);
            if (method != null)
                return method;
        }

        return null;
    }

    private static MethodInfo? FindMethod(Assembly assembly, string typeName, string methodName)
    {
        var type = assembly.GetType(typeName);

        var method = type?.GetMethod(methodName);
        return method;
    }
}

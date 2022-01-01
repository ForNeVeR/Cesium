using System.Reflection;
using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

public static class TypeSystemEx
{
    public static MethodReference? MethodLookup(this TranslationUnitContext context, CliImportSpecifier specifier)
    {
        var memberName = specifier.MemberName;
        var components = memberName.Split("::", 2);
        if (components.Length != 2)
            throw new NotSupportedException($"Invalid CLI member name: {memberName}.");

        var typeName = components[0];
        var methodName = components[1];

        var method = FindMethod(context.AssemblyContext.ImportAssemblies, typeName, methodName);
        if (method == null) return null;

        return context.Module.ImportReference(method);
    }

    private static MethodInfo? FindMethod(Assembly[] assemblies, string typeName, string methodName)
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
        if (type == null) return null;

        var method = type.GetMethod(methodName);
        return method;
    }
}

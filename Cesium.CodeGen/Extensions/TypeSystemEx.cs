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

        // TODO: Lookup in referenced assemblies.
        var runtimeAssembly = typeof(Console).Assembly;
        var type = runtimeAssembly.GetType(typeName);
        if (type == null) return null;
        var method = type.GetMethod(methodName);
        if (method == null) return null;

        return context.Module.ImportReference(method);
    }
}

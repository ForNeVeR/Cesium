using System.Reflection;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Extensions;

internal static class TypeSystemEx
{
    public static MethodReference MethodLookup(
        this TranslationUnitContext context,
        string memberName,
        ParametersInfo parametersInfo,
        IType returnType)
    {
        var components = memberName.Split("::", 2);
        if (components.Length != 2)
            throw new NotSupportedException($"Invalid CLI member name: {memberName}.");

        var typeName = components[0];
        var methodName = components[1];

        // TODO[#161]: Method search should be implemented in Cecil, to not load the assemblies into the current process.
        var candidates = FindMethods(context.AssemblyContext.ImportAssemblies, typeName, methodName).ToList();
        var similarMethods = new List<(MethodInfo, string)>();
        foreach (var candidate in candidates)
        {
            if (Match(context, candidate, parametersInfo, returnType, similarMethods))
            {
                return context.Module.ImportReference(candidate);
            }
        }

        var paramsString = string.Join(", ", parametersInfo.Parameters.Select(x => x.Type.Resolve(context)));
        var methodDisplayName = $"{memberName}({paramsString})";
        var errorMessage = similarMethods.Count == 0
            ? $"Cannot find CLI-imported member {methodDisplayName}."
            : SimilarMethodsMessage(methodDisplayName, similarMethods);

        throw new NotSupportedException(errorMessage);
    }

    private static IEnumerable<MethodInfo> FindMethods(IEnumerable<Assembly> assemblies, string typeName, string methodName)
    {
        return assemblies.SelectMany(assembly =>
        {
            var type = assembly.GetType(typeName);
            return type == null
                ? Array.Empty<MethodInfo>()
                : type.GetMethods().Where(m => m.Name == methodName);
        });
    }

    /// <summary>
    /// <para>
    /// Returns <c>true</c> if the method completely matches the parameter set. Otherwise, returns <c>false</c>, and
    /// optionally adds method to <paramref name="similarMethods"/> with a corresponding explanation.
    /// </para>
    /// <para>Not every case deserves an explanation.</para>
    /// </summary>
    private static bool Match(
        TranslationUnitContext context,
        MethodInfo method,
        ParametersInfo parameters,
        IType returnType,
        List<(MethodInfo, string)> similarMethods)
    {
        var declParamCount = parameters switch
        {
            {IsVoid: true} => 0,
            {IsVarArg: true} => parameters.Parameters.Count + 1,
            _ => parameters.Parameters.Count
        };

        var methodParameters = method.GetParameters();
        if (methodParameters.Length != declParamCount)
        {
            return false;
        }

        var declReturnReified = returnType.Resolve(context);
        if (declReturnReified.FullName != method.ReturnType.FullName)
        {
            similarMethods.Add((method, $"Returns types do not match: {declReturnReified.Name} in declaration, {method.ReturnType.Name} in source."));
            return false;
        }

        for (var i = 0; i < parameters.Parameters.Count; i++)
        {
            var declParam = parameters.Parameters[i];
            var declParamType = declParam.Type.Resolve(context);

            var srcParam = methodParameters[i];
            var srcParamType = srcParam.ParameterType;

            if (declParamType.FullName != srcParamType.FullName)
            {
                similarMethods.Add((method, $"Type of argument #{i} does not match: {declParamType} in declaration, {srcParamType} in source."));
                return false;
            }
        }

        if (parameters.IsVarArg)
        {
            var lastSrcParam = methodParameters.Last();
            // TODO[#161]: Should actually be imported to Cecil type universe, context.Module.ImportReference(typeof(ParamArrayAttribute))
            var paramsAttrType = typeof(ParamArrayAttribute);
            if (lastSrcParam.ParameterType.IsArray == false
                || lastSrcParam.CustomAttributes.Any(x => x.AttributeType == paramsAttrType) == false)
            {
                similarMethods.Add((method, $"Signature does not match: accepts variadic arguments in declaration, but not in source."));
                return false;
            }
        }

        // sic! no backwards check: if the last argument is a params array in source, and a plain array in declaration, it's safe to pass it as is
        return true;
    }

    private static string SimilarMethodsMessage(string name, List<(MethodInfo, string)> similarMethods)
    {
        return $"Cannot find an appropriate overload for CLI-imported function {name}. Candidates:\n"
               + string.Join("\n", similarMethods.Select(pair =>
                   {
                       var (method, message) = pair;
                       return $"{method}: {message}";
                   }
               ));
    }
}

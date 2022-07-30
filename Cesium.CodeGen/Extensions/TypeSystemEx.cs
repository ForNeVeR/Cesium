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

    public static bool IsEqualTo(this TypeReference a, TypeReference b) => a.FullName == b.FullName;

    public static bool IsSignedInteger(this TypeSystem ts, TypeReference t)
    {
        return t.IsEqualTo(ts.SByte)
            || t.IsEqualTo(ts.Int16)
            || t.IsEqualTo(ts.Int32)
            || t.IsEqualTo(ts.Int64);
    }

    public static bool IsUnsignedInteger(this TypeSystem ts, TypeReference t)
    {
        return t.IsEqualTo(ts.Byte)
            || t.IsEqualTo(ts.UInt16)
            || t.IsEqualTo(ts.UInt32)
            || t.IsEqualTo(ts.UInt64);
    }

    public static bool IsFloatingPoint(this TypeSystem ts, TypeReference t) => t.IsEqualTo(ts.Double) || t.IsEqualTo(ts.Single);
    public static bool IsInteger(this TypeSystem ts, TypeReference t) => ts.IsSignedInteger(t) || ts.IsUnsignedInteger(t);
    public static bool IsNumeric(this TypeSystem ts, TypeReference t) => ts.IsInteger(t) || ts.IsFloatingPoint(t);

    public static TypeReference GetCommonNumericType(this TypeSystem ts, TypeReference a, TypeReference b)
    {
        // First, if the corresponding real type of either operand is (long) double,
        // the other operand is converted, without change of type domain, to a type whose corresponding real type is (long) double.
        if (a.IsEqualTo(ts.Double) || b.IsEqualTo(ts.Double))
            return ts.Double;

        // Otherwise, if the corresponding real type of either operand is float,
        // the other operand is converted, without change of type domain, to a type whose corresponding real type is float.
        if (a.IsEqualTo(ts.Single) || b.IsEqualTo(ts.Single))
            return ts.Single;

        // Otherwise, if both operands have signed integer types or both have unsigned integer types,
        // the operand with the type of lesser integer conversion rank is converted to the type of the operand with greater rank.
        var signedTypes = new[] {ts.SByte, ts.Int16, ts.Int32, ts.Int64};
        var unsignedTypes = new[] {ts.Byte, ts.UInt16, ts.UInt32, ts.UInt64};

        var aSignedRank = RankOf(a, signedTypes);
        var bSignedRank = RankOf(b, signedTypes);

        if (aSignedRank != null && bSignedRank != null)
            return signedTypes[Math.Max(aSignedRank.Value, bSignedRank.Value)];

        var aUnsignedRank = RankOf(a, unsignedTypes);
        var bUnsignedRank = RankOf(b, unsignedTypes);

        if (aUnsignedRank != null && bUnsignedRank != null)
            return unsignedTypes[Math.Max(aUnsignedRank.Value, bUnsignedRank.Value)];

        if (aSignedRank == null && aUnsignedRank == null)
            throw new NotSupportedException($"Left operand of type {a.Name} is not numeric.");

        if (bSignedRank == null && bUnsignedRank == null)
            throw new NotSupportedException($"Right operand of type {b.Name} is not numeric.");

        // Otherwise, if the operand that has unsigned integer type has rank greater or equal to the rank of the type of the other operand,
        // then the operand with signed integer type is converted to the type of the operand with unsigned integer type.
        // Otherwise, if the type of the operand with signed integer type can represent all of the values of the type of the operand with unsigned integer type,
        // then the operand with unsigned integer type is converted to the type of the operand with signed integer type.

        var unsignedRank = aUnsignedRank ?? bUnsignedRank ?? throw new Exception("Not possible");
        var signedRank = aSignedRank ?? bSignedRank ?? throw new Exception("Not possible");

        return unsignedRank >= signedRank
            ? unsignedTypes[unsignedRank]
            : signedTypes[signedRank];

        // Otherwise, both operands are converted to the unsigned integer type corresponding to the type of the operand with signed integer type.
        // ^^ this doesn't seem to be possible with .NET types, because: Byte.MaxValue < Int16.MaxValue, UInt16.MaxValue < Int32.MaxValue, etc.

        int? RankOf(TypeReference t, TypeReference[] family)
        {
            for(var i = 0; i < family.Length; i++)
                if (t.IsEqualTo(family[i]))
                    return i;
            return null;
        }
    }
}

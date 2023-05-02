using System.Runtime.Versioning;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
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
            throw new CompilationException($"Invalid CLI member name: {memberName}.");

        var typeName = components[0];
        var methodName = components[1];

        var candidates = FindMethods(context.AssemblyContext.ImportAssemblies, typeName, methodName).ToList();
        var similarMethods = new List<(MethodDefinition, string)>();
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

        throw new CompilationException(errorMessage);
    }

    private static IEnumerable<MethodDefinition> FindMethods(IEnumerable<AssemblyDefinition> assemblies, string typeName, string methodName)
    {
        return assemblies.SelectMany(assembly =>
        {
            var type = assembly.GetType(typeName);
            return type == null
                ? Array.Empty<MethodDefinition>()
                : type.Methods.Where(m => m.Name == methodName);
        });
    }

    public static IType MakePointerType(this IType type)
    {
        return new Ir.Types.PointerType(type);
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
        MethodDefinition method,
        ParametersInfo parameters,
        IType returnType,
        List<(MethodDefinition, string)> similarMethods)
    {
        var declParamCount = parameters switch
        {
            {IsVoid: true} => 0,
            {IsVarArg: true} => parameters.Parameters.Count + 1,
            _ => parameters.Parameters.Count
        };

        var methodParameters = method.Parameters;
        if (methodParameters.Count != declParamCount)
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
            if (parameters.Parameters.Count + 1 != method.Parameters.Count)
            {
                similarMethods.Add((method, $"Signature does not match: accepts variadic arguments in declaration, but not in source. Count of parameters does not match."));
                return false;
            }

            var lastSrcParam = methodParameters.Last();
            if (lastSrcParam.ParameterType is not Mono.Cecil.PointerType pointerType)
            {
                similarMethods.Add((method, $"Signature does not match: accepts variadic arguments in declaration, but not in source. Last parameter is not an pointer type."));
                return false;
            }

            if (!pointerType.ElementType.IsEqualTo(context.TypeSystem.Void))
            {
                similarMethods.Add((method, $"Signature does not match: accepts variadic arguments in declaration, but not in source. Last parameter is not an void*."));
                return false;
            }
        }

        // sic! no backwards check: if the last argument is a params array in source, and a plain array in declaration, it's safe to pass it as is
        return true;
    }

    private static string SimilarMethodsMessage(string name, List<(MethodDefinition, string)> similarMethods)
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
    public static bool IsEqualTo(this IType a, IType b) => a.Equals(b);

    public static bool IsSignedInteger(this CTypeSystem ts, IType t)
    {
        return t.IsEqualTo(ts.SignedChar)
            || t.IsEqualTo(ts.Short)
            || t.IsEqualTo(ts.Int)
            || t.IsEqualTo(ts.Long)
            || t.IsEqualTo(ts.NativeInt);
    }

    public static bool IsUnsignedInteger(this CTypeSystem ts, IType t)
    {
        return t.IsEqualTo(ts.Bool)
            || t.IsEqualTo(ts.Char)
            || t.IsEqualTo(ts.UnsignedShort)
            || t.IsEqualTo(ts.UnsignedInt)
            || t.IsEqualTo(ts.UnsignedLong)
            || t.IsEqualTo(ts.NativeUInt);
    }

    public static bool IsFloatingPoint(this CTypeSystem ts, IType t) => t.IsEqualTo(ts.Double) || t.IsEqualTo(ts.Float);
    public static bool IsInteger(this CTypeSystem ts, IType t) => ts.IsSignedInteger(t) || ts.IsUnsignedInteger(t);
    public static bool IsNumeric(this CTypeSystem ts, IType t) => ts.IsInteger(t) || ts.IsFloatingPoint(t);
    public static bool IsBool(this CTypeSystem ts, IType t) => t.IsEqualTo(ts.Bool);


    /// <remarks>See 6.3.1.8 Usual arithmetic conversions in the C standard.</remarks>
    public static IType GetCommonNumericType(this CTypeSystem ts, IType a, IType b)
    {
        // First, if the corresponding real type of either operand is (long) double,
        // the other operand is converted, without change of type domain, to a type whose corresponding real type is (long) double.
        if (a.IsEqualTo(ts.Double) || b.IsEqualTo(ts.Double))
            return ts.Double;

        // Otherwise, if the corresponding real type of either operand is float,
        // the other operand is converted, without change of type domain, to a type whose corresponding real type is float.
        if (a.IsEqualTo(ts.Float) || b.IsEqualTo(ts.Float))
            return ts.Float;

        // Otherwise, if both operands have signed integer types or both have unsigned integer types,
        // the operand with the type of lesser integer conversion rank is converted to the type of the operand with greater rank.
        var signedTypes = new[] {ts.SignedChar, ts.Short, ts.Int, ts.Long, ts.NativeInt};
        var unsignedTypes = new[] { ts.Char, ts.UnsignedShort, ts.UnsignedInt, ts.UnsignedLong, ts.NativeUInt};
        // TODO: Move NativeInt and NativeUInt accordingly or consider them properly based on the current architecture.

        var aSignedRank = RankOf(a, signedTypes);
        var bSignedRank = RankOf(b, signedTypes);

        if (aSignedRank != null && bSignedRank != null)
            return signedTypes[Math.Max(aSignedRank.Value, bSignedRank.Value)];

        var aUnsignedRank = RankOf(a, unsignedTypes);
        var bUnsignedRank = RankOf(b, unsignedTypes);

        if (aUnsignedRank != null && bUnsignedRank != null)
            return unsignedTypes[Math.Max(aUnsignedRank.Value, bUnsignedRank.Value)];

        if (aSignedRank == null && aUnsignedRank == null)
            throw new AssertException($"Left operand of type {a} is not numeric.");

        if (bSignedRank == null && bUnsignedRank == null)
            throw new AssertException($"Right operand of type {b} is not numeric.");

        // Otherwise, if the operand that has unsigned integer type has rank greater or equal to the rank of the type of the other operand,
        // then the operand with signed integer type is converted to the type of the operand with unsigned integer type.
        // Otherwise, if the type of the operand with signed integer type can represent all of the values of the type of the operand with unsigned integer type,
        // then the operand with unsigned integer type is converted to the type of the operand with signed integer type.

        var unsignedRank = aUnsignedRank ?? bUnsignedRank ?? throw new AssertException("Not possible");
        var signedRank = aSignedRank ?? bSignedRank ?? throw new AssertException("Not possible");

        return unsignedRank >= signedRank
            ? unsignedTypes[unsignedRank]
            : signedTypes[signedRank];

        // Otherwise, both operands are converted to the unsigned integer type corresponding to the type of the operand with signed integer type.
        // ^^ this doesn't seem to be possible with .NET types, because: Byte.MaxValue < Int16.MaxValue, UInt16.MaxValue < Int32.MaxValue, etc.

        int? RankOf(IType t, IType[] family)
        {
            for(var i = 0; i < family.Length; i++)
                if (t.IsEqualTo(family[i]))
                    return i;
            return null;
        }
    }

    public static TypeDefinition GetRuntimeHelperType(this TranslationUnitContext context)
    {
        var runtimeHelpersType = context.AssemblyContext.CesiumRuntimeAssembly.GetType("Cesium.Runtime.RuntimeHelpers");
        return runtimeHelpersType ?? throw new AssertException("Type Cesium.Runtime.RuntimeHelpers was not found in the Cesium runtime assembly.");
    }

    public static MethodReference GetRuntimeHelperMethod(this TranslationUnitContext context, string helperMethod)
    {
        var runtimeHelpersType = context.GetRuntimeHelperType();
        var method = runtimeHelpersType.FindMethod(helperMethod);
        if (method == null)
        {
            throw new AssertException($"RuntimeHelper {helperMethod} cannot be found.");
        }

        return context.Module.ImportReference(method);
    }

    public static MethodReference GetArrayCopyToMethod(this TranslationUnitContext context)
    {
        return context.Module.ImportReference(typeof(byte*[]).GetMethod("CopyTo", new[] { typeof(Array), typeof(int) }));
    }
    public static MethodReference GetTargetFrameworkAttributeConstructor(this TranslationUnitContext context)
    {
        var constructor = typeof(TargetFrameworkAttribute).GetConstructor(new[] { typeof(string) });
        return context.Module.ImportReference(constructor);
    }
}

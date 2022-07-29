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

    public static bool IsFloatingPoint(this TypeSystem ts, TypeReference t)
    {
        return t.IsEqualTo(ts.Double)
            || t.IsEqualTo(ts.Single);
    }

    public static bool IsInteger(this TypeSystem ts, TypeReference t) => ts.IsSignedInteger(t) || ts.IsUnsignedInteger(t);

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

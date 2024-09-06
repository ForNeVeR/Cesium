using System.Runtime.Versioning;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using PointerType = Mono.Cecil.PointerType;

namespace Cesium.CodeGen.Extensions;

internal static class TypeSystemEx
{
    public const string CPtrFullTypeName = "Cesium.Runtime.CPtr`1";
    public const string VoidPtrFullTypeName = "Cesium.Runtime.VoidPtr";
    public const string FuncPtrFullTypeName = "Cesium.Runtime.FuncPtr`1";
    public const string EquivalentTypeAttributeName = "Cesium.Runtime.Attributes.EquivalentTypeAttribute";

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
            { IsVoid: true } => 0,
            { IsVarArg: true } => parameters.Parameters.Count + 1,
            _ => parameters.Parameters.Count
        };

        var methodParameters = method.Parameters;
        if (methodParameters.Count != declParamCount)
        {
            return false;
        }

        var declReturnReified = returnType.Resolve(context);
        if (!TypesCorrespond(context.TypeSystem, declReturnReified, method.ReturnType))
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

            if (!TypesCorrespond(context.TypeSystem, declParamType, srcParamType))
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

    /// <summary>Determines whether the types correspond to each other.</summary>
    /// <remarks>
    /// This tries to handle the pointer interop between the arch-independent pointer types introduced by the Cesium
    /// compatibility model and the actual runtime pointer types.
    /// </remarks>
    private static bool TypesCorrespond(TypeSystem typeSystem, TypeReference type1, TypeReference type2)
    {
        // let type 1 to be pointer out of these two
        if (type2.IsPointer || type2.IsFunctionPointer) (type1, type2) = (type2, type1);
        var isType1AnyPointer = type1.IsPointer || type1.IsFunctionPointer;
        var isType2AnyPointer = type2.IsPointer || type2.IsFunctionPointer;
        if (!isType1AnyPointer || isType2AnyPointer)
        {
            // If type1 is not a pointer, then we don't need to use the compatibility model for this type pair.
            // If type2 is a pointer, then type1 is also a pointer, and so no compatibility is required as well.
            return type1.FullName == type2.FullName;
        }

        if (type2.FullName.Equals(VoidPtrFullTypeName))
        {
            return type1 is PointerType pt && pt.ElementType.IsEqualTo(typeSystem.Void);
        }

        var resolvedType2 = type2.Resolve();
        if (resolvedType2.HasCustomAttributes)
        {
            // check for EquivalentTypeAttribute
            foreach(var attr in resolvedType2.CustomAttributes)
            {
                if (!attr.AttributeType.FullName.Equals(EquivalentTypeAttributeName))
                    continue;

                var eqType = (TypeReference)attr.ConstructorArguments[0].Value;
                if (type1.FullName == eqType.FullName)
                    return true;
            }
        }

        if (type2 is not GenericInstanceType type2Instance) return false;
        var type2Definition = type2.GetElementType();
        if (type1.IsPointer)
        {
            if (type2Definition.FullName != CPtrFullTypeName)
            {
                // Non-pointer gets compared to a pointer.
                return false;
            }

            var pointed1 = ((PointerType)type1).ElementType;
            var pointed2 = type2Instance.GenericArguments.Single();
            return TypesCorrespond(typeSystem, pointed1, pointed2);
        }

        if (type1.IsFunctionPointer)
        {
            if (type2Definition.FullName != FuncPtrFullTypeName)
            {
                // A function pointer gets compared to not a function pointer.
                return false;
            }

            // TODO[#490]: Compare the function type signatures here.
            return true;
        }

        throw new AssertException("Impossible: type1 should be either a pointer or a function pointer.");
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

    public static bool IsCArray(this TypeReference tr) => tr.Name.StartsWith("<SyntheticBuffer>");

    public static bool IsEqualTo(this TypeReference a, TypeReference b) => a.FullName == b.FullName;
    public static bool IsEqualTo(this IType a, IType b) => a.Equals(b);

    public static bool IsSignedInteger(this IType t)
    {
        return t.IsEqualTo(CTypeSystem.SignedChar)
            || t.IsEqualTo(CTypeSystem.Short)
            || t.IsEqualTo(CTypeSystem.Int)
            || t.IsEqualTo(CTypeSystem.Long)
            || t.IsEqualTo(CTypeSystem.NativeInt);
    }

    public static bool IsUnsignedInteger(this IType t)
    {
        return t.IsEqualTo(CTypeSystem.Bool)
            || t.IsEqualTo(CTypeSystem.Char)
            || t.IsEqualTo(CTypeSystem.UnsignedChar)
            || t.IsEqualTo(CTypeSystem.UnsignedShort)
            || t.IsEqualTo(CTypeSystem.Unsigned)
            || t.IsEqualTo(CTypeSystem.UnsignedInt)
            || t.IsEqualTo(CTypeSystem.UnsignedLong)
            || t.IsEqualTo(CTypeSystem.NativeUInt);
    }

    public static bool IsFloatingPoint(this IType t) => t.IsEqualTo(CTypeSystem.Double) || t.IsEqualTo(CTypeSystem.Float);
    public static bool IsInteger(this IType t) => t.IsSignedInteger() || t.IsUnsignedInteger();
    public static bool IsNumeric(this IType t) => t.IsInteger() || t.IsFloatingPoint() || t.IsEnum();
    public static bool IsBool(this IType t) => t.IsEqualTo(CTypeSystem.Bool);
    public static bool IsVoid(this IType t) => t.IsEqualTo(CTypeSystem.Void);
    public static bool IsEnum(this IType t) => t is EnumType;


    /// <remarks>See 6.3.1.8 Usual arithmetic conversions in the C standard.</remarks>
    public static IType GetCommonNumericType(IType a, IType b)
    {
        // First, if the corresponding real type of either operand is (long) double,
        // the other operand is converted, without change of type domain, to a type whose corresponding real type is (long) double.
        if (a.IsEqualTo(CTypeSystem.Double) || b.IsEqualTo(CTypeSystem.Double))
            return CTypeSystem.Double;

        // Otherwise, if the corresponding real type of either operand is float,
        // the other operand is converted, without change of type domain, to a type whose corresponding real type is float.
        if (a.IsEqualTo(CTypeSystem.Float) || b.IsEqualTo(CTypeSystem.Float))
            return CTypeSystem.Float;

        if (a.IsEqualTo(CTypeSystem.Bool))
            return b;

        if (b.IsEqualTo(CTypeSystem.Bool))
            return a;

        // Otherwise, if both operands have signed integer types or both have unsigned integer types,
        // the operand with the type of lesser integer conversion rank is converted to the type of the operand with greater rank.
        var signedTypes = new[] { CTypeSystem.SignedChar, CTypeSystem.Short, CTypeSystem.Int, CTypeSystem.Long, CTypeSystem.NativeInt};
        var unsignedTypes = new[] { CTypeSystem.Char, CTypeSystem.UnsignedChar, CTypeSystem.UnsignedShort, CTypeSystem.Unsigned, CTypeSystem.UnsignedInt, CTypeSystem.UnsignedLong, CTypeSystem.NativeUInt };
        // TODO[#381]: Move NativeInt and NativeUInt accordingly or consider them properly based on the current architecture.

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
            for (var i = 0; i < family.Length; i++)
                if (t.IsEqualTo(family[i]))
                    return i;
            return null;
        }
    }

    public static IType EraseConstType(this IType a)
    {
        if (a is ConstType constType)
        {
            return EraseConstType(constType.Base);
        }

        return a;
    }

    public static TypeDefinition GetRuntimeHelperType(this TranslationUnitContext context)
    {
        var runtimeHelpersType = context.AssemblyContext.CesiumRuntimeAssembly.GetType("Cesium.Runtime.RuntimeHelpers");
        return runtimeHelpersType ?? throw new AssertException("Type Cesium.Runtime.RuntimeHelpers was not found in the Cesium runtime assembly.");
    }

    public static MethodReference GetRuntimeHelperMethod(this TranslationUnitContext context, string helperMethod)
    {
        var runtimeHelpersType = context.GetRuntimeHelperType();
        var method = runtimeHelpersType.FindMethod(helperMethod) ?? throw new AssertException($"RuntimeHelper {helperMethod} cannot be found.");
        return context.Module.ImportReference(method);
    }

    public static MethodReference GetArrayCopyToMethod(this TranslationUnitContext context)
    {
        var typeSystem = context.Module.TypeSystem;
        var arrayRef = context.Module.ImportReference(new TypeReference("System", "Array", context.Module, typeSystem.CoreLibrary));
        var copyToMethodRef = new MethodReference("CopyTo", typeSystem.Void, arrayRef);
        copyToMethodRef.HasThis = true;
        copyToMethodRef.Parameters.Add(new ParameterDefinition(arrayRef));
        copyToMethodRef.Parameters.Add(new ParameterDefinition(typeSystem.Int32));
        copyToMethodRef = context.Module.ImportReference(copyToMethodRef);

        return context.Module.ImportReference(copyToMethodRef);
    }
    public static MethodReference GetTargetFrameworkAttributeConstructor(this TranslationUnitContext context)
    {
        var constructor = typeof(TargetFrameworkAttribute).GetConstructor(new[] { typeof(string) });
        return context.Module.ImportReference(constructor);
    }

    public static MethodReference? FindConversionFrom(this TypeReference actualArg, TypeReference passedArg, TranslationUnitContext context)
    {
#if !RESOLUTION_CESIUM
        var conversion = new MethodReference("op_Implicit", actualArg, actualArg); // Gentlemen are taken at their word.
        conversion.Parameters.Add(new(passedArg));
        return conversion;
#else
        var argumentType = actualArg.Resolve();
        var conversion = argumentType.Methods.FirstOrDefault(method => method.Name == "op_Implicit" &&
            method.ReturnType.IsEqualTo(actualArg) && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.IsEqualTo(passedArg));
        if (conversion == null)
            return null;

        return context.Module.ImportReference(conversion);
#endif
    }

    public static MethodReference? FindConversionTo(this TypeReference actualArg, TypeReference passedArg, TranslationUnitContext context)
    {
        var conversion = new MethodReference("op_Implicit", passedArg, actualArg); // Gentlemen are taken at their word.
        conversion.Parameters.Add(new(actualArg));
        return conversion;
#if !RESOLUTION_CESIUM
#else
        var argumentType = actualArg.Resolve();
        var conversion = argumentType.Methods.FirstOrDefault(method => method.Name == "op_Implicit" &&
            method.ReturnType.IsEqualTo(passedArg) && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.IsEqualTo(actualArg));
        if (conversion == null)
            return null;

        return context.Module.ImportReference(conversion);
#endif
    }
}

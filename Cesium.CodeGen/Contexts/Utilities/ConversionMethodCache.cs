using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Contexts.Utilities;

/// <remarks>
/// Currently specialized for operators that look like this:
/// <code>
/// class {GenericType}&lt;T>
/// {
///     public static {GenericType}&lt;T> {MethodName}({any argument});
/// }
/// </code>
/// </remarks>
internal record ConversionMethodCache(
    TypeReference GenericType,
    TypeReference? ReturnType,
    string MethodName,
    ModuleDefinition TargetModule)
{
    private readonly Dictionary<string, MethodReference> _methods = new();
    private readonly object _lock = new();
    public MethodReference GetOrImportMethod(TypeReference argument)
    {
        var fqn = argument.FullName;
        lock (_lock)
        {
            if (_methods.GetValueOrDefault(fqn) is { } method) return method;
            return _methods[fqn] = FindMethod(argument);
        }
    }

    private MethodReference FindMethod(TypeReference argument)
    {
        var genericMethod = GenericType.Resolve().Methods.Single(m => m.Name == MethodName);
        var declaringType = GenericType.MakeGenericInstanceType(argument);
        var methodReference = TargetModule.ImportReference(genericMethod);
        methodReference.DeclaringType = declaringType;
        if (ReturnType != null)
            methodReference.ReturnType = ReturnType;

        return methodReference;
    }
}

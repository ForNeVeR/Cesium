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
        var methodReference = new MethodReference(
            genericMethod.Name,
            returnType: GenericType.MakeGenericInstanceType(
                GenericType.GenericParameters.Single()),
            declaringType);
        foreach (var p in genericMethod.Parameters)
        {
            methodReference.Parameters.Add(
                new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
        }

        return TargetModule.ImportReference(methodReference);
    }
}

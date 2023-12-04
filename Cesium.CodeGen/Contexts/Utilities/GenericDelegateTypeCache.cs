using System.Globalization;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts.Utilities;

internal record GenericDelegateTypeCache(
    string Namespace,
    string TypeName,
    ModuleDefinition TargetModule)
{
    private readonly object _delegateCacheLock = new();
    private readonly Dictionary<int, TypeReference> _cache = new();
    public TypeReference GetDelegateType(int typeArgumentCount)
    {
        lock (_delegateCacheLock)
        {
            if (_cache.GetValueOrDefault(typeArgumentCount) is { } result) return result;
            return _cache[typeArgumentCount] = FindDelegate(typeArgumentCount);
        }
    }

    private TypeReference FindDelegate(int typeArgumentCount)
    {
        var realTypeName = $"{TypeName}`{typeArgumentCount.ToString(CultureInfo.InvariantCulture)}";
        var type = new TypeReference(
            Namespace,
            realTypeName,
            null,
            TargetModule.TypeSystem.CoreLibrary);
        for (var i = 0; i < typeArgumentCount; ++i)
            type.GenericParameters.Add(new GenericParameter(type));

        return TargetModule.ImportReference(type);
    }
}

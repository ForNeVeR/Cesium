namespace Cesium.Preprocessor;

public class InMemoryDefinesContext : IDefinesContext
{
    private readonly Dictionary<string, string?> defines;

    public InMemoryDefinesContext(IReadOnlyDictionary<string, string?>? initialDefines = null)
    {
        defines = initialDefines == null ? new Dictionary<string, string?>() : new Dictionary<string, string?>(initialDefines);
    }

    public void Define(string macro, string? replacement)
    {
        defines[macro] = replacement;
    }

    public bool TryGetDefine(string macro, out string? macroReplacement)
    {
        return defines.TryGetValue(macro, out macroReplacement);
    }
}

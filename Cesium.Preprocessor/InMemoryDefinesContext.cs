using System.Diagnostics.CodeAnalysis;

namespace Cesium.Preprocessor;

public class InMemoryDefinesContext : IMacroContext
{
    private readonly Dictionary<string, string?> defines;

    public InMemoryDefinesContext(IReadOnlyDictionary<string, string?>? initialDefines = null)
    {
        defines = initialDefines == null ? new Dictionary<string, string?>() : new Dictionary<string, string?>(initialDefines);
    }

    public void DefineMacro(string macro, string? replacement)
    {
        defines[macro] = replacement;
    }

    public bool TryResolveMacro(string macro, [NotNullWhen(true)]out string? macroReplacement)
    {
        return defines.TryGetValue(macro, out macroReplacement);
    }
}

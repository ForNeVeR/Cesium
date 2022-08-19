using System.Diagnostics.CodeAnalysis;

namespace Cesium.Preprocessor;

public interface IMacroContext
{
    bool TryResolveMacro(string macro, [NotNullWhen(true)]out string? macroReplacement);

    void DefineMacro(string macro, string? replacement);
}

namespace Cesium.Preprocessor;

public interface IMacroContext
{
    bool TryResolveMacro(string macro, out string? macroReplacement);

    void DefineMacro(string macro, string? replacement);
}

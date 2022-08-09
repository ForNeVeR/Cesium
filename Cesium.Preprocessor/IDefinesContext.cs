namespace Cesium.Preprocessor;

public interface IDefinesContext
{
    bool TryGetDefine(string macro, out string? macroReplacement);
    void Define(string macro, string? replacement);
}

using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public class InMemoryDefinesContext : IMacroContext
{
    private readonly Dictionary<string, IList<IToken<CPreprocessorTokenType>>> _defines;
    private readonly Dictionary<string, IList<string>> _defineParameters;

    public InMemoryDefinesContext(IReadOnlyDictionary<string, IList<IToken<CPreprocessorTokenType>>>? initialDefines = null)
    {
        _defines = initialDefines == null
            ? new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>()
            : new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>(initialDefines);
        _defineParameters = new Dictionary<string, IList<string>>();
    }

    public void DefineMacro(string macro, string[]? parameters, IList<IToken<CPreprocessorTokenType>> replacement)
    {
        _defines[macro] = replacement;
        if (parameters is { })
        {
            _defineParameters[macro] = parameters;
        }
    }

    public void UndefineMacro(string macro)
    {
        _defines.Remove(macro);
        _defineParameters.Remove(macro);
    }

    public bool TryResolveMacro(string macro, out IList<string>? macroParameters, [NotNullWhen(true)]out IList<IToken<CPreprocessorTokenType>>? macroReplacement)
    {
        _defineParameters.TryGetValue(macro, out macroParameters);
        return _defines.TryGetValue(macro, out macroReplacement);
    }
}

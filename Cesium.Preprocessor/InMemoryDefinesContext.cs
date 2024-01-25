using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public class InMemoryDefinesContext : IMacroContext
{
    private readonly Dictionary<string, IList<IToken<CPreprocessorTokenType>>> _defines;
    private readonly Dictionary<string, MacroParameters?> _defineMacros;

    public InMemoryDefinesContext(
        IReadOnlyDictionary<string, IList<IToken<CPreprocessorTokenType>>>? initialDefines = null)
    {
        _defines = initialDefines == null
            ? new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>()
            : new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>(initialDefines);
        _defineMacros = new();

        DefineMacro(
            "__LINE__",
            parameters: null,
            replacement: []);

        DefineMacro(
            "__FILE__",
            parameters: null,
            replacement: []);
    }

    public void DefineMacro(string macro, MacroParameters? parameters, IList<IToken<CPreprocessorTokenType>> replacement)
    {
        _defines[macro] = replacement;
        _defineMacros[macro] = parameters;
    }

    public void UndefineMacro(string macro)
    {
        _defines.Remove(macro);
        _defineMacros.Remove(macro);
    }

    public bool TryResolveMacro(string macro, out MacroParameters? parameters, [NotNullWhen(true)]out IList<IToken<CPreprocessorTokenType>>? macroReplacement)
    {
        // TODO: Either add an assertion that the dictionaries are synchronized, or merge them into one dictionary with
        // a struct as the value.
        _defineMacros.TryGetValue(macro, out parameters);
        return _defines.TryGetValue(macro, out macroReplacement);
    }
}

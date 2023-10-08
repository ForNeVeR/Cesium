using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public class InMemoryDefinesContext : IMacroContext
{
    private readonly Dictionary<string, IList<IToken<CPreprocessorTokenType>>> _defines;
    private readonly Dictionary<string, MacroDefinition> _defineMacros;

    public InMemoryDefinesContext(IReadOnlyDictionary<string, IList<IToken<CPreprocessorTokenType>>>? initialDefines = null)
    {
        _defines = initialDefines == null
            ? new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>()
            : new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>(initialDefines);
        _defineMacros = new();
    }

    public void DefineMacro(string macro, MacroDefinition macroDefinition, IList<IToken<CPreprocessorTokenType>> replacement)
    {
        _defines[macro] = replacement;
        _defineMacros[macro] = macroDefinition;
    }

    public void UndefineMacro(string macro)
    {
        _defines.Remove(macro);
        _defineMacros.Remove(macro);
    }

    public bool TryResolveMacro(string macro, out MacroDefinition? macroDefinition, [NotNullWhen(true)]out IList<IToken<CPreprocessorTokenType>>? macroReplacement)
    {
        _defineMacros.TryGetValue(macro, out macroDefinition);
        return _defines.TryGetValue(macro, out macroReplacement);
    }
}

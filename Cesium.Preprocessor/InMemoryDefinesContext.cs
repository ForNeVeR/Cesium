using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public class InMemoryDefinesContext : IMacroContext
{
    private record struct Macro(
        MacroParameters? Parameters,
        IList<IToken<CPreprocessorTokenType>> Replacement
    );

    private readonly Dictionary<string, Macro> _macros = new();

    public InMemoryDefinesContext()
    {
        DefineMacro(
            "__LINE__",
            parameters: null,
            replacement: []);

        DefineMacro(
            "__FILE__",
            parameters: null,
            replacement: []);

        DefineMacro(
            "__CESIUM__",
            parameters: null,
            replacement: []);
    }

    public void DefineMacro(string macro, MacroParameters? parameters, IList<IToken<CPreprocessorTokenType>> replacement)
    {
        _macros[macro] = new Macro(parameters, replacement);
    }

    public void UndefineMacro(string macro)
    {
        _macros.Remove(macro);
    }

    public bool TryResolveMacro(
        string name,
        out MacroParameters? parameters,
        [NotNullWhen(true)] out IList<IToken<CPreprocessorTokenType>>? replacement)
    {
        var exists = _macros.TryGetValue(name, out var macro);
        parameters = macro.Parameters;
        replacement = macro.Replacement;
        return exists;
    }
}

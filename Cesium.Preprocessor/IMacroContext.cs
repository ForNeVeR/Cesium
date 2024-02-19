using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public interface IMacroContext
{
    bool TryResolveMacro(
        string name,
        out MacroParameters? parameters,
        [NotNullWhen(true)] out IList<IToken<CPreprocessorTokenType>>? replacement);

    void DefineMacro(string macro, MacroParameters? parameters, IList<IToken<CPreprocessorTokenType>> replacement);
    void UndefineMacro(string macro);
}

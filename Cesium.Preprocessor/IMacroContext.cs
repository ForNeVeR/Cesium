using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public interface IMacroContext
{
    bool TryResolveMacro(string macro, out IList<string>? macroParameters, [NotNullWhen(true)]out IList<IToken<CPreprocessorTokenType>>? macroReplacement);

    void DefineMacro(string macro, string[]? parameters, IList<IToken<CPreprocessorTokenType>> replacement);
    void UndefineMacro(string macro);
}

using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

public interface IMacroContext
{
    bool TryResolveMacro(string macro, out MacroDefinition? macroDefinition, [NotNullWhen(true)]out IList<IToken<CPreprocessorTokenType>>? macroReplacement);

    void DefineMacro(string macro, MacroDefinition macroDefinition, IList<IToken<CPreprocessorTokenType>> replacement);
    void UndefineMacro(string macro);
}

using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;

[Parser(typeof(CPreprocessorTokenType))]
internal partial class CPreprocessorMacroDefinitionParser
{
    [Rule("macro: PreprocessingToken")]
    private static MacroDefinition MakeIdentifier(ICPreprocessorToken macroName) => new MacroDefinition(macroName.Text, null);

    [Rule("macro: PreprocessingToken '(' (PreprocessingToken (',' PreprocessingToken)*) ')'")]
    private static MacroDefinition MakeIdentifier(
        ICPreprocessorToken macroName,
        ICPreprocessorToken openParen,
        Punctuated<ICPreprocessorToken, ICPreprocessorToken> parameteres,
        ICPreprocessorToken closeParen) => new MacroDefinition(macroName.Text, parameteres.Values.Select(_ => _.Text).ToArray());

    public record MacroDefinition(string Name, string[]? Parameters);
}

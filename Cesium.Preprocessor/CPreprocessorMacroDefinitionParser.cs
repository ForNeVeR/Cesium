using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;

[Parser(typeof(CPreprocessorTokenType))]
internal partial class CPreprocessorMacroDefinitionParser
{
    [Rule("macro: WhiteSpace PreprocessingToken WhiteSpace?")]
    private static MacroDefinition MakeIdentifier(ICPreprocessorToken whitespace, ICPreprocessorToken macroName, ICPreprocessorToken? whitespace2)
        => new MacroDefinition(macroName.Text, null);

    [Rule("wrapped_token: WhiteSpace? PreprocessingToken WhiteSpace?")]
    private static ICPreprocessorToken MakeWrappedToke(ICPreprocessorToken whitespace, ICPreprocessorToken token, ICPreprocessorToken? whitespace2)
        => token;

    [Rule("macro: WhiteSpace PreprocessingToken WhiteSpace? '(' (wrapped_token (',' wrapped_token)*) ')' WhiteSpace?")]
    private static MacroDefinition MakeIdentifier(
        ICPreprocessorToken whitespace,
        ICPreprocessorToken macroName,
        ICPreprocessorToken? whitespace_,
        ICPreprocessorToken openParen,
        Punctuated<ICPreprocessorToken, ICPreprocessorToken> parameteres,
        ICPreprocessorToken closeParen,
        ICPreprocessorToken? whitespace2) => new MacroDefinition(macroName.Text, parameteres.Values.Select(_ => _.Text).ToArray());

    public record MacroDefinition(string Name, string[]? Parameters);
}

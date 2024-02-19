using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Lexer.Attributes;

namespace Cesium.Preprocessor;

public enum CPreprocessorTokenType
{
    [Error] Error,
    [End] End,

    [Regex("[ \t\v\f]")] WhiteSpace,
    [Regex(Regexes.LineComment)]
    [Regex(Regexes.MultilineComment)]
    Comment,
    [Regex("\r|\n|\r\n")] NewLine,

    [Token("#")] Hash,
    [Token("##")] DoubleHash,
    [Regex(@"\\")] NextLine,

    [Regex("<[^\r\n>]+>")]
    [Regex("\"[^\r\n\"]+\"")]
    HeaderName,

    [Token("...")]
    Ellipsis,

    [Regex("[^ \t\v\f\r\n#;+\\-*/()=!<\",.|&\\\\]+")]
    PreprocessingToken,

    [Regex(@"([.]|[;+\\-*/=!,|&]+|<=|>=|>|<)")]
    Separator,

    [Token("(")]
    LeftParen,

    [Token(")")]
    RightParen,
}

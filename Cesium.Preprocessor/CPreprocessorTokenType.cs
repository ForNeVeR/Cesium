using Yoakke.Lexer;
using Yoakke.Lexer.Attributes;

namespace Cesium.Preprocessor;

public enum CPreprocessorTokenType
{
    [Error] Error,
    [End] End,

    [Regex("[ \t\v\f]")] WhiteSpace,
    [Regex(Regexes.LineComment)]
    [Regex(Regexes.MultilineComment)]
    Comment,
    [Regex("[\r\n]+")] NewLine,

    [Token("#")] Hash,
    [Token("##")] DoubleHash,

    // TODO:
    // preprocessing-token:
    //     header-name
    //     identifier
    //     pp-number
    //     character-constant
    //     string-literal
    //     punctuator
    //     each non-white-space character that cannot be one of the above
    [Regex("<[^\r\n>]+>")]
    [Regex("\"[^\r\n\"]+\"")]
    HeaderName,

    // [Regex(@"(([0-9]+.?)|([0-9]*(.[0-9]+)))([eE][+-]?[0-9]+)?")]
    // Number,

    [Regex("[^ \t\v\f\r\n#]+")]
    PreprocessingToken
}

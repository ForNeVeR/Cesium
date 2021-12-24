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

    [Regex("<[^\r\n>]+>")]
    [Regex("\"[^\r\n\"]+\"")]
    HeaderName,

    [Regex("[^ \t\v\f\r\n#]+")]
    PreprocessingToken
}

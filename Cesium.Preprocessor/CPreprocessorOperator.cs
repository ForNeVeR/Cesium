using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

internal enum CPreprocessorOperator
{
    Equals,
    NotEquals,
    LessOrEqual,
    GreaterOrEqual,
    LessThen,
    GreaterThen,
    Negation,
    LogicalOr,
    LogicalAnd,
}

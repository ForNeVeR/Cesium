using Yoakke.Lexer;
using Yoakke.Lexer.Attributes;

namespace Cesium.Lexer;

/// <remarks>(6.4) token</remarks>
public enum TokenType
{
    [Error] Error,
    [End] End,

    /// <remarks>(6.4.1) keyword</remarks>
    [Token("auto")]
    [Token("break")]
    [Token("case")]
    [Token("char")]
    [Token("const")]
    [Token("continue")]
    [Token("default")]
    [Token("do")]
    [Token("double")]
    [Token("else")]
    [Token("enum")]
    [Token("extern")]
    [Token("float")]
    [Token("for")]
    [Token("goto")]
    [Token("if")]
    [Token("inline")]
    [Token("int")]
    [Token("long")]
    [Token("register")]
    [Token("restrict")]
    [Token("return")]
    [Token("short")]
    [Token("signed")]
    [Token("sizeof")]
    [Token("static")]
    [Token("struct")]
    [Token("switch")]
    [Token("typedef")]
    [Token("union")]
    [Token("unsigned")]
    [Token("void")]
    [Token("volatile")]
    [Token("while")]
    [Token("_Alignas")]
    [Token("_Alignof")]
    [Token("_Atomic")]
    [Token("_Bool")]
    [Token("_Complex")]
    [Token("_Generic")]
    [Token("_Imaginary")]
    [Token("_Noreturn")]
    [Token("_Static_assert")]
    [Token("_Thread_local")]
    Keyword,

    /// <remarks>A.1.3 Identifiers</remarks>
    [Regex(Regexes.Identifier)] // TODO: Support Unicode and universal-character-name.
    Identifier,

    /// <remarks>A.1.5 Constants</remarks>
    [Regex(Regexes.IntLiteral)] // TODO: Support octal and hex
    // TODO: Review this and add other types
    Constant,

    /// <remarks>A.1.6 String literals</remarks>
    [Regex(Regexes.StringLiteral)] // TODO: Support prefixes
    StringLiteral,

    [Regex(@"\[|\]|\(|\)|\{|\}|.|->|\+\+|--|&|\*|\+|-|~|!|/|%|<<|>>|<|>|<=|>=|==|!=|^|\||&&|\|\||\?|:|;|...|=|\*=|/=|%=|\+=|-=|<<=|>>=|&=|^=|\|=|,|#|##|<:|:>|<%|%>|%:|%:%:")]
    Punctuator,

    /// <remarks>6.4.9 Comments</remarks>
    [Ignore]
    [Regex(Regexes.LineComment)] // TODO: Support block comments, too.
    Comment,

    /// <remarks>6.4 Lexical elements: Semantics, paragraph 3</remarks>
    [Ignore]
    [Regex("[ \t\n\v\r]")] // TODO: Support Unicode whitespace
    WhiteSpace
}

/// <remarks>(6.4) preprocessing-token</remarks>
// TODO: Support this
public enum PreprocessingToken
{
    HeaderName,
    Identifier,
    PpNumber,
    CharacterConstant,
    StringLiteral,
    Punctuator
}

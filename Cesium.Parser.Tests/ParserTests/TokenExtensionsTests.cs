// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Parser.Tests.ParserTests;

public class TokenExtensionsTests
{
    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"\\n\"", "\n")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\\\x10\"", "\\x10")]
    [InlineData("\"\\x20\"", " ")]
    [InlineData("\"\\\\00\"", "\\00")]
    [InlineData("\"\\00\"", "\0")]
    [InlineData("\"\\0\"", "\0")]
    [InlineData("\"\\1\"", "\x01")]
    [InlineData("\"\\12\"", "\n")]
    [InlineData("\"\\123\"", "S")]
    [InlineData("\"\\x\"", "\\x")]
    public void Test(string tokenText, string expected)
    {
        var token = new Token<CTokenType>(new Range(), new Location(), tokenText, CTokenType.StringLiteral);

        var actual = token.UnwrapStringLiteral();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("\"\\z\"")]
    [InlineData("\"\\j\"")]
    [InlineData("\"\\9\"")]
    [InlineData("\"\\ \"")]
    public void InvalidEscapeSequenceThrows(string tokenText)
    {
        var token = new Token<CTokenType>(new Range(), new Location(), tokenText, CTokenType.StringLiteral);

        var ex = Assert.Throws<ParseException>(token.UnwrapStringLiteral);
        Assert.Contains("Unrecognized escape sequence", ex.Message);
    }
}

// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Preprocessor;

/// <summary>
/// A macro name that was not replaced because it was found during the replacement of the same macro.
/// </summary>
/// <remarks>
/// C23 Standard, section 6.10.4.4 Rescanning and further replacement: such tokens "are no longer available for
/// further replacement even if they are later (re)examined in contexts in which that macro name preprocessing token
/// would otherwise have been replaced".
/// </remarks>
internal sealed class NonReplaceableToken(IToken<CPreprocessorTokenType> token) : IToken<CPreprocessorTokenType>
{
    public Range Range => token.Range;
    public Location Location => token.Location;
    public string Text => token.Text;
    public CPreprocessorTokenType Kind => token.Kind;

    public bool Equals(IToken? other) => token.Equals(other);
    public bool Equals(IToken<CPreprocessorTokenType>? other) => token.Equals(other);
}

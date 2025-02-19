// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

internal static class LexerExtensions
{
    public static IEnumerable<IToken<T>> ToEnumerableUntilEnd<T>(this ILexer<IToken<T>> lexer)
    {
        while (!lexer.IsEnd)
            yield return lexer.Next();
    }
}

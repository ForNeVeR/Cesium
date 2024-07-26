using System.Runtime.CompilerServices;
using System.Text;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Parser;

public static class TokenExtensions
{
    public unsafe static string UnwrapStringLiteral(this IToken<CTokenType> token)
    {
        if (token.Kind != CTokenType.StringLiteral)
            throw new ParseException($"Non-string literal token: {token.Kind} {token.Text}");

        var result = token.Text[1..^1]; // allocates new sliced string

        if (result.IndexOf('\\') == -1) // no \ no fun no unescape
            return result;

        fixed (char* p = result)
        {
            var span = new Span<char>(p, result.Length + 1); // create a span for string. Also +1 for \0
            int eaten = 0;
            while (true) // loop
            {
                var i = span.IndexOf('\\'); // SIMD search \. Blazing fast.
                if (i == -1) break; // break if there is no more \

                int shift = 1; // how many characters we're gonna skip

                switch (span[i + 1])
                {
                    // Simple escape sequences
                    case '"': span[i] = '"'; break;
                    case '?': span[i] = '?'; break;
                    case '\'': span[i] = '\''; break;
                    case '\\': span[i] = '\\'; break;
                    case 'a': span[i] = '\a'; break;
                    case 'b': span[i] = '\b'; break;
                    case 'f': span[i] = '\f'; break;
                    case 'n': span[i] = '\n'; break;
                    case 'r': span[i] = '\r'; break;
                    case 't': span[i] = '\t'; break;
                    case 'v': span[i] = '\v'; break;
                    // Numeric escape sequences
                    case '0': // arbitrary octal value '\nnn'
                        {
                            if (span.Length <= i + 2 || span[i + 2] == '\0') // \0 check for 2nd..n iters.
                            {
                                span[i] = '\0';
                                break;
                            }

                            int number = 0;
                            var c = span[i + shift + 1]; // get next char after 0
                            do
                            {
                                number = number * 8 + (c - '0');
                                shift++;
                                c = span[i + shift + 1];
                            }
                            while (char.IsBetween(c, '0', '7'));
                            span[i] = (char)number;
                            break;
                        }
                    case 'x':
                    case 'X': // \Xn... arbitrary hexadecimal value
                        {
                            if (span.Length <= i + 2 || span[i + 2] == '\0') // \0 check for 2nd..n iters.
                            {
                                shift = 0;
                                break;
                            }

                            int number = 0;
                            var c = span[i + 1 + shift]; // shift == 1, so i + 2 points at next char after '\' 'X'
                            do
                            {
                                int digit = char.IsAsciiDigit(c) ? c - '0' : (char.ToUpperInvariant(c) - 'A') + 10;
                                number = number * 16 + digit;
                                shift++;
                                c = span[i + 1 + shift];
                            }
                            while (char.IsAsciiDigit(c) || char.IsBetween(c, 'a', 'f') || char.IsBetween(c, 'A', 'F'));
                            span[i] = (char)number;
                            break;
                        }
                    // Universal character names
                    case 'u': // \unnnn
                    case 'U': // \Unnnnnnnn 
                        {
                            int counter = span[i + 1] == 'U' ? 8 : 4;
                            if (span.Length <= i + counter) // no free chars no fun
                            {
                                shift = 0;
                                break;
                            }

                            int number = 0;
                            for (int n = 0; n < counter; n++)
                            {
                                var c = span[i + 2 + n]; // i + 2 points at next char after '\', 'U',
                                // in theory, we should throw an error.
                                if (!(char.IsAsciiDigit(c) || char.IsBetween(c, 'a', 'f') || char.IsBetween(c, 'A', 'F'))) break;
                                int digit = char.IsAsciiDigit(c) ? c - '0' : (char.ToUpperInvariant(c) - 'A') + 10;
                                number = number * 16 + digit;
                                shift++;
                            }
                            // char.ConvertFromUtf32(number) allocates string :(
                            //                create span  from     pointer    to our number  represented as ref byte
                            var spanCodeSeq = new Span<byte>(Unsafe.AsPointer(ref Unsafe.As<int, byte>(ref number)), 4);
                            // get utf16 chars from utf32 bytes seq without allocations yeeeah
                            if (!Encoding.UTF32.TryGetChars(spanCodeSeq, span.Slice(i), out int written)) throw new Exception("Bad UTF32 sequence!");
                            // if we writted one char, so just do nothing
                            // if we writted two chars, so just skip one char
                            i += written - 1;
                            shift -= written - 1;
                            break;
                        }
                    default:
                        // from orig method:
                        // TODO[#295]: maybe smarter handling of this edge case with errors/warnings
                        // builder.Append('\\');
                        // --i; // don't skip next
                        // mmm, idk when that might happen
                        break;
                }

                if (shift == 0)
                {
                    span = span.Slice(1); // skip 1 char
                    continue;
                }

                // ex:
                // before
                // span[i] = '\'  span[i+1] = 'n' span[i+2] = 'h'
                // after
                // shift = 1
                // span[i] = '\n' span[i+1] = 'n' span[i+2] = 'h'
                // it is required to shift all subsequent chars by (1..shift) to the left
                // result slice slice copy
                // span[i] = '\n' span[i+1] = 'h' span[i+2] = (previous span)[i+3]
                var start = i + shift;
                var source = span.Slice(start + 1); // next from consumed chars
                var destination = span.Slice(i + 1); // next from span[i] = '\n'
                // If copy properly in chunks, rather than continuously copying the WHOLE unchecked string, unescape will be even faster
                source.CopyTo(destination); // <--- copies the WHOLE unchecked string. bad. bad. but also SIMDed, so it's still very fast
                span = destination; // next iter
                eaten += shift;
            }

            // oh wait
            // before: "kek \\n\\n\\n kek"    len 17
            // after   "kek \n\n\n kek\0\0\0" len 17
            // create copy & alocate it again
            // we can change the length of an existing string, but GC always *bonk* for it
            result = new ReadOnlySpan<char>(p, result.Length - eaten).ToString(); // alocate x2 ><
            // after:  "kek \n\n\n kek" len 14

            return result;
        }
    }
}

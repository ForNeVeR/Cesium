#if NETSTANDARD
using System;
using System.Text;
#else
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public unsafe static class StringFunctions
{
    public static nuint StrLen(byte* str)
    {
        var offset = StrChr(str, 0);
        return (nuint)(offset - str);
    }
    public static byte* StrCpy(byte* dest, byte* src)
    {
        if (dest == null)
        {
            return null;
        }

        var result = dest;
        if (src == null)
        {
            return dest;
        }

        byte* search = src;
        while (*search != '\0')
        {
            *dest = *search;
            search++;
            dest++;
        }

        *dest = 0;
        return result;
    }
    public static byte* StrNCpy(byte* dest, byte* src, nuint count)
    {
        if (dest == null)
        {
            return null;
        }

        var result = dest;
        if (src == null)
        {
            return dest;
        }

        uint counter = 0;
        byte* search = src;
        while (*search != '\0')
        {
            *dest = *search;
            search++;
            dest++;
            counter++;
            if (counter == count)
            {
                break;
            }
        }

        return result;
    }
    public static byte* StrCat(byte* dest, byte* src)
    {
        if (dest == null)
        {
            return null;
        }

        var result = dest;
        if (src == null)
        {
            return dest;
        }

        while (*dest != '\0')
        {
            dest++;
        }

        byte* search = src;
        while (*search != '\0')
        {
            *dest = *search;
            search++;
            dest++;
        }

        *dest = 0;
        return result;
    }
    public static byte* StrNCat(byte* dest, byte* src, nuint count)
    {
        if (dest == null)
        {
            return null;
        }

        var result = dest;
        if (src == null)
        {
            return dest;
        }

        while (*dest != '\0')
        {
            dest++;
        }

        uint counter = 0;
        byte* search = src;
        while (*search != '\0' && counter < count)
        {
            *dest = *search;
            search++;
            dest++;
            counter++;
        }

        *dest = 0;
        return result;
    }
    public static int StrNCmp(byte* lhs, byte* rhs, nuint count)
    {
        if (lhs == null || rhs == null)
        {
            return -1;
        }

        var result = lhs;
        if (count == 0)
        {
            return 0;
        }

        for (nuint i = 0; i < count; i++, lhs++, rhs++)
        {
            int diff = *lhs - *rhs;
            if (diff != 0)
            {
                return diff;
            }
        }

        return 0;
    }

    public static void* Memset(void* dest, int ch, nuint count)
    {
        byte* val = (byte*)dest;
        while (count > 0)
        {
            *val = (byte)ch;
            count--;
        }

        return dest;
    }
    public static byte* StrChr(byte* str, int ch)
    {
        if (str != null)
        {
            int match;
            nuint offset = 0;
            byte c = (byte)ch;

            // TODO: Copy IndexOfNullByte impl. from CoreLib in a distant future
            var span = new Span<byte>(str, int.MaxValue);
            while ((match = span.IndexOf(c)) < 0)
            {
                offset += int.MaxValue;
                span = new Span<byte>(str + offset, int.MaxValue);
            }

            return str + ((uint)match + offset);
        }

        return null;
    }
}

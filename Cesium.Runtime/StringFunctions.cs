#if NETSTANDARD
using System.Text;
#else
using System.Collections.Specialized;
using System.Runtime.InteropServices;
#endif

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public static unsafe class StringFunctions
{
    public static nuint StrLen(byte* str)
    {
#if NETSTANDARD
        if (str == null)
        {
            return 0;
        }

        Encoding encoding = Encoding.UTF8;
        int byteLength = 0;
        byte* search = str;
        while (*search != '\0')
        {
            byteLength++;
            search++;
        }

        int stringLength = encoding.GetCharCount(str, byteLength);
        return (uint)stringLength;
#else
        return (uint)(Marshal.PtrToStringUTF8((nint)str)?.Length ?? 0);
#endif
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
        if (str == null)
        {
            return null;
        }

        while (*str != 0)
        {
            if (*str == ch)
            {
                return str;
            }

            str++;
        }

        return null;
    }
}

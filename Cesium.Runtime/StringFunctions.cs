#if NETSTANDARD
using System.Drawing;
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
    public static nuint StrLen(UTF8String str) => str.Length;

    public static byte* StrCpy(UTF8String dest, UTF8String src)
    {
        if (!dest)
            return null;

        if (!src)
            return dest;

        src.CopyTo(dest);

        return dest;
    }

    public static byte* StrNCpy(UTF8String dest, UTF8String src, nuint count)
    {
        if (!dest)
            return null;

        if (!src)
            return dest;

        src.CopyTo(dest, count);

        return dest;
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
    public static byte* StrChr(UTF8String str, int ch)
    {
        if (!str)
            return null;

        return str.FindEntry((byte)ch);
    }

    public static int StrCmp(byte* lhs, byte* rhs)
    {
        if (lhs is null) return -1;
        if (rhs is null) return -1;

        for (; *lhs != 0 && *rhs != 0; lhs++, rhs++)
        {
            if (*lhs < *rhs) return -1;
            if (*lhs > *rhs) return 1;
        }


        if (*lhs < *rhs) return -1;
        if (*lhs > *rhs) return 1;
        return 0;
    }

    public static int StrCmpS(byte* lhs, byte* rhs, nuint count)
    {
        if (lhs is null) return -1;
        if (rhs is null) return -1;

        for (; *lhs != 0 && *rhs != 0 && count != 0; lhs++, rhs++, count--)
        {
            if (*lhs < *rhs) return -1;
            if (*lhs > *rhs) return 1;
        }

        if (*lhs < *rhs) return -1;
        if (*lhs > *rhs) return 1;
        return 0;
    }

    public static int MemCmp(void* lhs, void* rhs, nuint count)
    {
        if (lhs is null) return -1;
        if (rhs is null) return -1;

        byte* lhs_ = (byte*)lhs;
        byte* rhs_ = (byte*)rhs;

        for (; *lhs_ != 0 && *rhs_ != 0 && count != 0; lhs_++, rhs_++, count--)
        {
            if (*lhs_ < *rhs_) return -1;
            if (*lhs_ > *rhs_) return 1;
        }

        return 0;
    }

    public static UTF8String StrDup(UTF8String src)
    {
        return StrNDup(src, src.Length);
    }

    public static UTF8String StrNDup(UTF8String src, nuint count)
    {
        if (src.Pointer == null)
            return UTF8String.NullString;

        var dest = new UTF8String((byte*)StdLibFunctions.Malloc(count + 1));
        src.CopyTo(dest, count);
        dest[count] = 0;
        return dest;
    }
}

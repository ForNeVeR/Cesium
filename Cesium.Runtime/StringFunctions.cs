// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

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

    public static UTF8String StrError(int errnum)
    {
        switch (errnum)
        {
            case ErrNo.EPERM:
                return CesiumFunctions.MarshalStr("ErrNo.EPERM");
            case ErrNo.ENOENT:
                return CesiumFunctions.MarshalStr("ErrNo.ENOENT");
            case ErrNo.ESRCH:
                return CesiumFunctions.MarshalStr("ErrNo.ESRCH");
            case ErrNo.EINTR:
                return CesiumFunctions.MarshalStr("ErrNo.EINTR");
            case ErrNo.EIO:
                return CesiumFunctions.MarshalStr("ErrNo.EIO");
            case ErrNo.ENXIO:
                return CesiumFunctions.MarshalStr("ErrNo.ENXIO");
            case ErrNo.E2BIG:
                return CesiumFunctions.MarshalStr("ErrNo.E2BIG");
            case ErrNo.ENOEXEC:
                return CesiumFunctions.MarshalStr("ErrNo.ENOEXEC");
            case ErrNo.EBADF:
                return CesiumFunctions.MarshalStr("ErrNo.EBADF");
            case ErrNo.ECHILD:
                return CesiumFunctions.MarshalStr("ErrNo.ECHILD");
            case ErrNo.EAGAIN:
                return CesiumFunctions.MarshalStr("ErrNo.EAGAIN");
            case ErrNo.ENOMEM:
                return CesiumFunctions.MarshalStr("ErrNo.ENOMEM");
            case ErrNo.EACCES:
                return CesiumFunctions.MarshalStr("ErrNo.EACCES");
            case ErrNo.EFAULT:
                return CesiumFunctions.MarshalStr("ErrNo.EFAULT");
            case ErrNo.EBUSY:
                return CesiumFunctions.MarshalStr("ErrNo.EBUSY");
            case ErrNo.EEXIST:
                return CesiumFunctions.MarshalStr("ErrNo.EEXIST");
            case ErrNo.EXDEV:
                return CesiumFunctions.MarshalStr("ErrNo.EXDEV");
            case ErrNo.ENODEV:
                return CesiumFunctions.MarshalStr("ErrNo.ENODEV");
            case ErrNo.ENOTDIR:
                return CesiumFunctions.MarshalStr("ErrNo.ENOTDIR");
            case ErrNo.EISDIR:
                return CesiumFunctions.MarshalStr("ErrNo.EISDIR");
            case ErrNo.ENFILE:
                return CesiumFunctions.MarshalStr("ErrNo.ENFILE");
            case ErrNo.EMFILE:
                return CesiumFunctions.MarshalStr("ErrNo.EMFILE");
            case ErrNo.ENOTTY:
                return CesiumFunctions.MarshalStr("ErrNo.ENOTTY");
            case ErrNo.EFBIG:
                return CesiumFunctions.MarshalStr("ErrNo.EFBIG");
            case ErrNo.ENOSPC:
                return CesiumFunctions.MarshalStr("ErrNo.ENOSPC");
            case ErrNo.ESPIPE:
                return CesiumFunctions.MarshalStr("ErrNo.ESPIPE");
            case ErrNo.EROFS:
                return CesiumFunctions.MarshalStr("ErrNo.EROFS");
            case ErrNo.EMLINK:
                return CesiumFunctions.MarshalStr("ErrNo.EMLINK");
            case ErrNo.EPIPE:
                return CesiumFunctions.MarshalStr("ErrNo.EPIPE");
            case ErrNo.EDOM:
                return CesiumFunctions.MarshalStr("ErrNo.EDOM");
            case ErrNo.EDEADLK:
                return CesiumFunctions.MarshalStr("ErrNo.EDEADLK");
            case ErrNo.ENAMETOOLONG:
                return CesiumFunctions.MarshalStr("ErrNo.ENAMETOOLONG");
            case ErrNo.ENOLCK:
                return CesiumFunctions.MarshalStr("ErrNo.ENOLCK");
            case ErrNo.ENOTEMPTY:
                return CesiumFunctions.MarshalStr("ErrNo.ENOTEMPTY");
            case ErrNo.EINVAL:
                return CesiumFunctions.MarshalStr("ErrNo.EINVAL");
            case ErrNo.ERANGE:
                return CesiumFunctions.MarshalStr("ErrNo.ERANGE");
            case ErrNo.EILSEQ:
                return CesiumFunctions.MarshalStr("ErrNo.EILSEQ");
            case ErrNo.STRUNCATE:
                return CesiumFunctions.MarshalStr("ErrNo.STRUNCATE");
            default:
                return UTF8String.NullString;
        }
    }

    public static UTF8String StrStr(UTF8String str, UTF8String substr)
    {
        if (substr[0] == '\0')
            return str;

        var start = str;
        var substrLength = substr.Length;
        while (!!start)
        {
            var candidate = start.FindEntry(substr[0]);
            if (!candidate)
            {
                return UTF8String.NullString;
            }

            if (StrNCmp(candidate, substr, substrLength) == 0)
            {
                return candidate;
            }

            start = candidate.At(1);
        }

        return UTF8String.NullString;
    }
}

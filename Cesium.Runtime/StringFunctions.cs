using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public static unsafe class StringFunctions
{
    public static nuint StrLen(byte* str)
    {
        if (str != null)
        {
            var start = str;
            while ((nuint)str % 16 != 0)
            {
                if (*str is 0)
                {
                    goto Done;
                }
                str++;
            }

            while (true)
            {
                var eqmask = Vector128.Equals(
                    Vector128.LoadAligned(str),
                    Vector128<byte>.Zero);
                if (eqmask == Vector128<byte>.Zero)
                {
                    str += Vector128<byte>.Count;
                    continue;
                }

                str += IndexOfMatch(eqmask);
                break;
            }
        Done:
            return (nuint)str - (nuint)start;
        }

        return 0;
    }
    public static byte* StrCpy(byte* dest, byte* src)
    {
        if (dest != null)
        {
            var result = dest;
            if (src != null)
            {
                // SIMD scan into SIMD copy (traversing the data twice)
                // is much faster than a single scalar check+copy loop.
                var length = StrLen(src);
                Buffer.MemoryCopy(src, dest, length, length);
                dest += length;
            }

            *dest = 0;
            return dest;
        }

        return null;
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
            byte c = (byte)ch;

            while ((nuint)str % 16 != 0)
            {
                var curr = *str;
                if (curr == c)
                {
                    goto Done;
                }
                else if (curr == 0)
                {
                    goto NotFound;
                }
                str++;
            }

            var element = Vector128.Create(c);
            var nullByte = Vector128<byte>.Zero;
            while (true)
            {
                var chars = Vector128.LoadAligned(str);
                var eqmask = Vector128.Equals(chars, element) |
                             Vector128.Equals(chars, nullByte);
                if (eqmask == Vector128<byte>.Zero)
                {
                    str += Vector128<byte>.Count;
                    continue;
                }

                str += IndexOfMatch(eqmask);
                if (*str == 0)
                {
                    goto NotFound;
                }
                break;
            }

        Done:
            return str;
        }

    NotFound:
        return null;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint IndexOfMatch(Vector128<byte> eqmask)
    {
        if (AdvSimd.Arm64.IsSupported)
        {
            var res = AdvSimd
                .ShiftRightLogicalNarrowingLower(eqmask.AsUInt16(), 4)
                .AsUInt64()
                .ToScalar();
            return (uint)BitOperations.TrailingZeroCount(res) >> 2;
        }

        return (uint)BitOperations.TrailingZeroCount(
            eqmask.ExtractMostSignificantBits());
    }
}

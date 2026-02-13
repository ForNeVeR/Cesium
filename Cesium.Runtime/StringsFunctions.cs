// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the strings.h
/// </summary>
public unsafe static class StringsFunctions
{
    public static int StrNCaseCmp(UTF8String s1, UTF8String s2, nuint size)
    {
        if (s1.Pointer == null)
        {
            return s2.Pointer == null ? 0 : 1;
        }
        else if (s2.Pointer == null)
        {
            return -1;
        }
        for (nuint i = 0; i < size; i++)
        {
            byte c1 = s1.At(i).Pointer[0];
            byte c2 = s2.At(i).Pointer[0];
            if (c1 >= 'A' && c1 <= 'Z')
            {
                c1 += 32;
            }
            if (c2 >= 'A' && c2 <= 'Z')
            {
                c2 += 32;
            }
            if (c1 != c2)
            {
                return c1 - c2;
            }
            if (c1 == 0)
            {
                break;
            }
        }
        return 0;
    }

    public static int StrCaseCmp(UTF8String s1, UTF8String s2)
    {
        if (s1.Pointer == null)
        {
            return s2.Pointer == null ? 0 : 1;
        }
        else if (s2.Pointer == null)
        {
            return -1;
        }
        for (nuint i = 0; ; i++)
        {
            byte c1 = s1.At(i).Pointer[0];
            byte c2 = s2.At(i).Pointer[0];
            if (c1 >= 'A' && c1 <= 'Z')
            {
                c1 += 32;
            }
            if (c2 >= 'A' && c2 <= 'Z')
            {
                c2 += 32;
            }
            if (c1 != c2)
            {
                return c1 - c2;
            }
            if (c1 == 0)
            {
                break;
            }
        }
        return 0;
    }
}

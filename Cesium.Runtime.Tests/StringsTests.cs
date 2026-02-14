// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Cesium.Runtime.Tests;
public unsafe class StringsTests
{
    [Fact]
    public void StrCaseCmpTest()
    {
        Assert.Equal(0, LocalTest("two", "TWO"));
        Assert.Equal(0, LocalTest("", ""));
        Assert.Equal(-4, LocalTest("nina", "NINE"));
        Assert.Equal(7, LocalTest("nine", "Night"));

        int LocalTest(string text, string text2)
        {
            UTF8String substr = (byte*)Marshal.StringToHGlobalAnsi($"{text}\0");
            UTF8String substr2 = (byte*)Marshal.StringToHGlobalAnsi($"{text2}\0");
            var result = StringsFunctions.StrCaseCmp(substr, substr2);
            return result;
        }
    }

    [Fact]
    public void StrNCaseCmpTest()
    {
        Assert.Equal(0, LocalTest("two", "TWO", 3));
        Assert.Equal(0, LocalTest("", "", 0));
        Assert.Equal(-4, LocalTest("nina", "NINE", 4));
        Assert.Equal(0, LocalTest("nine", "Night", 2));
        Assert.Equal(7, LocalTest("nine", "Night", 3));

        int LocalTest(string text, string text2, nuint size)
        {
            UTF8String substr = (byte*)Marshal.StringToHGlobalAnsi($"{text}\0");
            UTF8String substr2 = (byte*)Marshal.StringToHGlobalAnsi($"{text2}\0");
            var result = StringsFunctions.StrNCaseCmp(substr, substr2, size);
            return result;
        }
    }
}

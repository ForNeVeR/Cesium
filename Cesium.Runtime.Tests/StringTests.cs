// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Cesium.Runtime.Tests;
public unsafe class StringTests
{
    [Fact]
    public void StrSrtTest()
    {
        Assert.Equal(sizeof(long), sizeof(UTF8String));

        UTF8String someString = (byte*)Marshal.StringToHGlobalAnsi("one two three!\0");
        Assert.Equal(4L, LocalTest("two"));
        Assert.Equal(0L, LocalTest(""));
        Assert.Equal(-1, LocalTest("nine"));
        Assert.Equal(1, LocalTest("n"));

        long LocalTest(string text)
        {
            UTF8String substr = (byte*)Marshal.StringToHGlobalAnsi($"{text}\0");
            var result = StringFunctions.StrStr(someString, substr);
            if (!result) return -1;
            return (byte*)result - (byte*)someString;
        }
    }
}

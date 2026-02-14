// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Text;

namespace Cesium.Runtime.Tests;

public class StdLibFunctionTests
{
    [Theory]
    [InlineData("1010", 2, 10)]
    [InlineData("12", 8, 10)]
    [InlineData("A", 16, 10)]
    [InlineData("junk", 36, 926192)]
    [InlineData(" -40", 10, -40)]
    [InlineData("012", 0, 10)]
    [InlineData("0xA", 0, 10)]
    [InlineData("junk", 0, 0)]
    [InlineData("111.", 10, 111)]
    public unsafe void StrToL(string input, int @base, long expectedResult)
    {
        var stringBytes = Encoding.UTF8.GetBytes(input);
        fixed (byte* str = stringBytes)
        {
            var actual = StdLibFunctions.StrToL(str, null, @base);

            Assert.Equal(expectedResult, actual);
        }

        byte* strEnd;
        fixed (byte* str = stringBytes)
        {
            var actual = StdLibFunctions.StrToL(str, &strEnd, @base);

            Assert.Equal(expectedResult, actual);
        }
    }

    [Fact]
    public unsafe void StrToLOutOfRange()
    {
        var stringBytes = Encoding.UTF8.GetBytes("10 200000000000000000000000000000");
        fixed (byte* str = stringBytes)
        {
            byte* str_end;
            StdLibFunctions.StrToL(str, &str_end, 10);
            var actual = StdLibFunctions.StrToL(str_end, &str_end, 10);
            var errorCode = *StdLibFunctions.GetErrNo();
            Assert.Equal(34, errorCode);
            Assert.Equal(long.MaxValue, actual);
        }

        stringBytes = Encoding.UTF8.GetBytes(" -200000000000000000000000000000");
        fixed (byte* str = stringBytes)
        {
            var actual = StdLibFunctions.StrToL(str, null, 10);
            var errorCode = *StdLibFunctions.GetErrNo();
            Assert.Equal(34, errorCode);
            Assert.Equal(long.MinValue, actual);
        }
    }
    [Theory]
    [InlineData("111.11", 111.11)]
    [InlineData("-2.22", -2.22)]
    [InlineData("NaN", double.NaN)]
    [InlineData("nan(2)", double.NaN)]
    [InlineData("inF", double.PositiveInfinity)]
    [InlineData("0X1.BC70A3D70A3D7P+6", 111.11)]
    [InlineData("junk", 0)]
    public unsafe void StrToD(string input, double expectedResult)
    {
        var stringBytes = Encoding.UTF8.GetBytes(input);
        fixed (byte* str = stringBytes)
        {
            var actual = StdLibFunctions.StrToD(str, null);

            Assert.Equal(expectedResult, actual);
        }

        byte* strEnd;
        fixed (byte* str = stringBytes)
        {
            var actual = StdLibFunctions.StrToD(str, &strEnd);

            Assert.Equal(expectedResult, actual);
        }
    }
}

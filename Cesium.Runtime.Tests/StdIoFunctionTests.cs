using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Cesium.Runtime.Tests;

public class StdIoFunctionTests
{
    [Theory]
    [InlineData(9, "0x09")]
    [InlineData(32, "0x20")]
    public void FPrintFHex(long input, string expectedResult)
    {
        var (exitCode, result) = TestFPrintF("0x%02x", input);
        Assert.Equal(expectedResult, result);
        Assert.Equal(4, exitCode);
    }

    [Theory]
    [InlineData(-1L, "%li", 2, "-1")]
    [InlineData(-1L, "%lu", 20, "18446744073709551615")]
    [InlineData(-1L, "%zu", 20, "18446744073709551615")]
    public void FPrintFLong(long input, string format, int expectedExitCode, string expectedResult)
    {
        var (exitCode, result) = TestFPrintF(format, input);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedExitCode, exitCode);
    }

    private static unsafe (int, string) TestFPrintF(string format, long input)
    {
        var formatEncoded = Encoding.UTF8.GetBytes(format);

        int exitCode;
        var streamptr = (void*)IntPtr.Zero;
        using var buffer = new MemoryStream();
        try
        {
            using var writer = new StreamWriter(buffer);
            var handle = new StdIoFunctions.StreamHandle
            {
                FileMode = "w",
                // ReSharper disable once AccessToDisposedClosure
                Writer = () => writer
            };

            streamptr = StdIoFunctions.AddStream(handle);
            fixed (byte* formatPtr = formatEncoded)
            {
                exitCode = StdIoFunctions.FPrintF((void*)streamptr, formatPtr, &input);
            }
        }
        finally
        {
            StdIoFunctions.FreeStream(streamptr);
        }

        return (exitCode, Encoding.UTF8.GetString(buffer.ToArray()));
    }

    [Theory]
    [InlineData("a", "%c", 1, 'a')]
    [InlineData(" a\n", "%3c", 1, ' ', 'a', '\n')]
    [InlineData("a", "%*c", 0)]
    [InlineData("123     a", "%*d %c", 1, 'a')]
    [InlineData(" abc", "%*4c", 0)]
    public unsafe void FScanFChar(string input, string format, int expectedExitCode, params char[] expectedResult)
    {
        var chars = new byte[expectedResult.Length];
        var varargs = new long[1];

        int exitCode;
        fixed (byte* c = chars)
        {
            varargs[0] = (long)c;
            exitCode = TestFScanF(input, format, varargs);
        }

        var actual = Encoding.UTF8.GetString(chars);

        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(new string(expectedResult), actual);
    }

    [Theory]
    [InlineData("hello world", "%s", 1, "hello")]
    [InlineData("my name\nis", "%s%s%s", 3, "my", "name", "is")]
    [InlineData(" space", "%s", 1, "space")]
    [InlineData("123456", "%3s", 1, "123")]
    [InlineData("12 3456", "%3s", 1, "12")]
    [InlineData("prodotnet", "%3s %*3s %3s", 2, "pro", "net")]
    [InlineData(" ", "%s", -1)]
    public void FScanFString(string input, string format, int expectedExitCode, params string[] expectedResult)
    {
        var strings = expectedResult
            .Select(s => new byte[s.Length])
            .ToArray();

        var varargs = new long[strings.Length];
        var handles = new GCHandle[strings.Length];

        try
        {
            for (int i = 0; i < strings.Length; i++)
            {
                handles[i] = GCHandle.Alloc(strings[i], GCHandleType.Pinned);
                varargs[i] = (long)handles[i].AddrOfPinnedObject();
            }

            var exitCode = TestFScanF(input, format, varargs);

            var actual = strings.Select(b => Encoding.UTF8.GetString(b));

            Assert.Equal(expectedExitCode, exitCode);
            Assert.Equal(expectedResult, actual);
        }
        finally
        {
            for (int i = 0; i < handles.Length; i++)
                handles[i].Free();
        }
    }

    [Theory]
    [InlineData("+404", "%d", 1, 404)]
    [InlineData(" -12 00", "%2d%d", 2, -12, 0)]
    [InlineData("12:00", "%5d", 1, 12)]
    [InlineData("1", "%*d", 0)]
    [InlineData("abc", "%d", 0)]
    public void FScanFInt(string input, string format, int expectedExitCode, params int[] expectedResult)
        => FScanFStruct(input, format, expectedExitCode, expectedResult);

    [Theory]
    [InlineData("17179869184", "%lld", 1, 17179869184L)]
    [InlineData(" -1717986918400", "%11lld%lld", 2, -17179869184L, 0L)]
    [InlineData("17179869184:00", "%1000lld", 1, 17179869184L)]
    [InlineData("17179869184", "%*lld", 0)]
    [InlineData("abc", "%lld", 0)]
    public void FScanFLongLong(string input, string format, int expectedExitCode, params long[] expectedResult)
        => FScanFStruct(input, format, expectedExitCode, expectedResult);

    [Theory]
    [InlineData("AB4C", "%x", 1, 43852)]
    [InlineData("ABBA", "%2x%x", 2, 171, 186)]
    [InlineData("FF:00", "%5x", 1, 255)]
    [InlineData("FFFF", "%*x", 0)]
    [InlineData("GFFFF", "%x", 0)]
    public void FScanFHex(string input, string format, int expectedExitCode, params int[] expectedResult)
        => FScanFStruct(input, format, expectedExitCode, expectedResult);

    [Theory]
    [InlineData("0xFF", "%p", 1, 255)]
    [InlineData("0xFFFF", "%2p%p", 2, 255, 255)]
    [InlineData("XFF:00", "%10p", 1, 255)]
    [InlineData("0XFFFF", "%*p", 0)]
    [InlineData("0yFFFF", "%p", 0)]
    public void FScanFPointer(string input, string format, int expectedExitCode, params int[] expectedResult)
        => FScanFStruct(input, format, expectedExitCode, expectedResult);

    [Theory]
    [InlineData("123", "%o", 1, 83)]
    [InlineData("1213", "%2o%o", 2, 10, 11)]
    [InlineData("15:00", "%5o", 1, 13)]
    [InlineData("333", "%*o", 0)]
    [InlineData("8777", "%o", 0)]
    public void FScanFOct(string input, string format, int expectedExitCode, params int[] expectedResult)
        => FScanFStruct(input, format, expectedExitCode, expectedResult);

    [Theory]
    [InlineData("12", "%f", 1, 12f)]
    [InlineData("12.", "%f", 1, 12f)]
    [InlineData("12.000", "%f", 1, 12f)]
    [InlineData("12.13", "%f", 1, 12.13f)]
    [InlineData("54.32E-001", "%f", 1, 5.432f)]
    [InlineData("12.12e+002", "%*f", 0)]
    [InlineData("12..12", "%f", 0)]
    public void FScanFFloat(string input, string format, int expectedExitCode, params float[] expectedResult)
        => FScanFStruct(input, format, expectedExitCode, expectedResult);

    internal unsafe void FScanFStruct<T>(string input, string format, int expectedExitCode, params T[] expectedResult)
        where T : unmanaged
    {
        T[] nums = new T[expectedResult.Length];
        var varargs = new long[expectedResult.Length];

        int exitCode;
        fixed (T* _ = nums)
        {
            for (int i = 0; i < nums.Length; i++)
            {
                varargs[i] = (long)Unsafe.AsPointer(ref nums[i]);
            }
            exitCode = TestFScanF(input, format, varargs);
        }

        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(expectedResult, nums);
    }

    private static unsafe int TestFScanF(string input, string format, long[] varargs)
    {
        var argsPtr = GCHandle.Alloc(varargs, GCHandleType.Pinned).AddrOfPinnedObject();

        var formatEncoded = Encoding.UTF8.GetBytes(format);
        var inputSpan = input.ToCharArray();

        int exitCode;
        var streamPtr = (void*)IntPtr.Zero;

        using var buffer = new MemoryStream();

        try
        {
            using var reader = new StreamReader(buffer);

            var writer = new StreamWriter(buffer);
            writer.Write(inputSpan);
            writer.Flush();

            buffer.Seek(0, SeekOrigin.Begin);

            var handle = new StdIoFunctions.StreamHandle
            {
                FileMode = "r",
                // ReSharper disable once AccessToDisposedClosure
                Reader = () => reader
            };

            streamPtr = StdIoFunctions.AddStream(handle);
            fixed (byte* formatPtr = formatEncoded)
            {
                exitCode = StdIoFunctions.FScanF(streamPtr, formatPtr, (void*)argsPtr);
            }
        }
        finally
        {
            StdIoFunctions.FreeStream(streamPtr);
        }

        return exitCode;
    }
}

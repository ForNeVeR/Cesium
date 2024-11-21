using System.Text;

namespace Cesium.Runtime.Tests;

public class StdIoFunctionTests
{
    [Theory]
    [InlineData(9, "0x%02x", "0x09")]
    [InlineData(32, "0x%02x", "0x20")]
    [InlineData(10, "0x%02X", "0x0A")]
    [InlineData(10, "0x%02x", "0x0a")]
    public void FPrintFHex(long input, string format, string expectedResult)
    {
        var (exitCode, result) = TestFPrintF(format, input);
        Assert.Equal(expectedResult, result);
        Assert.Equal(4, exitCode);
    }

    [Theory]
    [InlineData(-1L, "%li", 2, "-1")]
    [InlineData(-1L, "%lu", 20, "18446744073709551615")]
    [InlineData(-1L, "%llu", 20, "18446744073709551615")]
    [InlineData(-1L, "%ull", 20, "18446744073709551615")]
    [InlineData(-1L, "%zu", 20, "18446744073709551615")]
    [InlineData(-1L, "%Lu", 20, "18446744073709551615")]
    [InlineData(-1L, "%LLu", 20, "18446744073709551615")]
    [InlineData(-1L, "%Ull", 20, "18446744073709551615")]
    public void FPrintFLong(long input, string format, int expectedExitCode, string expectedResult)
    {
        var (exitCode, result) = TestFPrintF(format, input);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedExitCode, exitCode);
    }

    [Theory]
    [InlineData(uint.MaxValue, "%u", 10, "4294967295")]
    [InlineData(uint.MaxValue, "%u\n", 11, "4294967295\n")]
    public void FPrintUInt(long input, string format, int expectedExitCode, string expectedResult)
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
}

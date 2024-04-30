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
        using var buffer = new MemoryStream();
        var handleIndex = StdIoFunctions.Handles.Count;
        try
        {
            using var writer = new StreamWriter(buffer);
            var handle = new StdIoFunctions.StreamHandle
            {
                FileMode = "w",
                // ReSharper disable once AccessToDisposedClosure
                Writer = () => writer
            };

            StdIoFunctions.Handles.Add(handle);
            fixed (byte* formatPtr = formatEncoded)
            {
                exitCode = StdIoFunctions.FPrintF((void*)handleIndex, formatPtr, &input);
            }
        }
        finally
        {
            StdIoFunctions.Handles.RemoveAt(handleIndex);
        }

        return (exitCode, Encoding.UTF8.GetString(buffer.ToArray()));
    }
}

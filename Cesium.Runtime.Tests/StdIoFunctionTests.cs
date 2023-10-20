using System.Text;

namespace Cesium.Runtime.Tests;

public class StdIoFunctionTests
{
    [Theory]
    [InlineData(9, "0x09")]
    [InlineData(32, "0x20")]
    public unsafe void FPrintFHex(long input, string expectedResult)
    {
        var format = Encoding.UTF8.GetBytes("0x%02x");

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
            fixed (byte* formatPtr = format)
            {
                Assert.Equal(4, StdIoFunctions.FPrintF((void*)handleIndex, formatPtr, &input));
            }
        }
        finally
        {
            StdIoFunctions.Handles.RemoveAt(handleIndex);
        }

        var result = Encoding.UTF8.GetString(buffer.ToArray());
        Assert.Equal(expectedResult, result);
    }
}

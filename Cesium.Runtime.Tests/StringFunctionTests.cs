using System.Text;

namespace Cesium.Runtime.Tests;

public unsafe class StringFunctionTests
{
    [Theory]
    [InlineData("Hello\0", 5)]
    [InlineData("Goodbye\0", 7)]
    [InlineData("Hello\0Goodbye\0", 5)]
    [InlineData("                  \0", 18)]
    public void StrLen(string input, int expected)
    {
        // TODO: If you are rich enough to procure a 2-4+ GB RAM runner,
        // please update this test to exercise the path where the string
        // length exceeds int.MaxLength of bytes.
        var bytes = Encoding.UTF8.GetBytes(input);
        fixed (byte* str = bytes)
        {
            var actual = StringFunctions.StrLen(str);

            Assert.Equal((nuint)expected, actual);
        }
    }

    [Theory]
    [InlineData("Hello\n", 5)]
    [InlineData("Goodbye\n", 7)]
    [InlineData("Hello\nGoodbye\n", 5)]
    [InlineData("                  \n", 18)]
    public void StrChr(string input, int expectedOffset)
    {
        var needle = '\n';
        var bytes = Encoding.UTF8.GetBytes(input);
        fixed (byte* str = bytes)
        {
            var ptr = StringFunctions.StrChr(str, '\n');

            Assert.Equal((byte)needle, *ptr);
            Assert.Equal(expectedOffset, (int)(ptr - str));
        }
    }
}

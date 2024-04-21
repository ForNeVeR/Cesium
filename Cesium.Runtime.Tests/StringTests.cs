using System.Runtime.InteropServices;

namespace Cesium.Runtime.Tests;
public unsafe class StringTests
{
    [Fact]
    public void ComplexTest()
    {
        Assert.Equal(sizeof(long), sizeof(UTF8String));

        UTF8String someString = (byte*)Marshal.StringToHGlobalAnsi("Hello world!\0");

        Assert.Equal("Hello world!".Length, (int)someString.Length);
        Assert.Equal("Hello world!".Length + 1, (int)someString.NullTerminatedLength);
        Assert.Equal("Hello world!", someString.ToString());

        UTF8String someMemory = stackalloc byte[(int)someString.NullTerminatedLength];
        someString.CopyTo(someMemory);
        Assert.Equal(someString.ToString(), someMemory.ToString());

        UTF8String someOtherMemory = stackalloc byte[(int)someString.NullTerminatedLength];
        someString.CopyTo(someOtherMemory, 5);
        someOtherMemory[6] = (byte)'\0';
        Assert.Equal("Hello", someOtherMemory.ToString());

        Assert.Equal((byte)'o', someString.At(4)[0]);
    }
}

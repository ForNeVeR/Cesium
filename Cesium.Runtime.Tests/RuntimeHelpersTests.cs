namespace Cesium.Runtime.Tests;

public class RuntimeHelpersTests
{
    [Fact]
    public unsafe void AllocateGlobalFieldTest()
    {
        const int size = 300;
        var memory = (byte*)RuntimeHelpers.AllocateGlobalField(size);
        try
        {
            for (var i = 0; i < size; ++i)
                memory[i] = (byte)i;

            for (var i = 0; i < size; ++i)
                Assert.Equal((byte)i, memory[i]);
        }
        finally
        {
            RuntimeHelpers.FreeGlobalField(memory);
        }
    }
}

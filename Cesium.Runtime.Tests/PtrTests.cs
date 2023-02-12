namespace Cesium.Runtime.Tests;

public unsafe class PtrTests
{
    [Fact]
    public void CPtrTests()
    {
        CPtr v = (void*)0x1234;
        Assert.Equal(0x1234L, (long)v.AsPtr());
        Assert.Equal(0x1234L, (long)v.AsPtr<byte>());

        Assert.Equal(sizeof(long), sizeof(CPtr));

        CPtr<int> t = (int*)0x2345;
        Assert.Equal(0x2345L, (long)t.AsPtr());
        Assert.Equal((IntPtr)0x2345, t.AsIntPtr());
        Assert.Equal(0x2345L, (long)t.AsPtr<byte>());

        Assert.Equal(sizeof(long), sizeof(CPtr<int>));
    }

    [Fact]
    public void FPtrTests()
    {
        var a = new FPtr<Action>((void*)0x1234);
        Assert.Equal(0x1234L, (long)a.AsPtr());
        Assert.Equal(sizeof(long), sizeof(FPtr<Action>));
    }
}

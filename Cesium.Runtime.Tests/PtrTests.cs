namespace Cesium.Runtime.Tests;

public unsafe class PtrTests
{
    [Fact]
    public void VoidPtrTests()
    {
        VoidPtr v = (void*)0x1234;
        Assert.Equal(0x1234L, (long)v.AsPtr());
        Assert.Equal(0x1234L, (long)v.AsPtr<byte>());

        Assert.Equal(sizeof(long), sizeof(VoidPtr));
    }

    [Fact]
    public void CPtrTests()
    {
        CPtr<int> t = (int*)0x2345;
        Assert.Equal(0x2345L, (long)t.AsPtr());
        Assert.Equal((IntPtr)0x2345, t.AsIntPtr());
        Assert.Equal(0x2345L, (long)t.AsPtr<byte>());

        Assert.Equal(sizeof(long), sizeof(CPtr<int>));
    }

    [Fact]
    public void FuncPtrTests()
    {
        var a = new FuncPtr<Action>((void*)0x1234);
        Assert.Equal(0x1234L, (long)a.AsPtr());
        Assert.Equal(sizeof(IntPtr), sizeof(FuncPtr<Action>));

        FuncPtr<Func<int>> funcPtr = (Func<int>)SomeAnonFunc;
        var func = SomeAnonFunc;
        Assert.Equal(funcPtr.AsDelegate()(), func());

        funcPtr = (delegate*<int>)&SomeAnonFunc;
        Assert.Equal(funcPtr.AsDelegate()(), func());

        static int SomeAnonFunc() => 5;
    }
}

namespace Cesium.Runtime;

/// <summary>A class encapsulating a C function pointer.</summary>
public readonly unsafe struct FuncPtr<TDelegate> where TDelegate : Delegate // TODO[#487]: Think about vararg and empty parameter list encoding.
{
    private readonly long _value;

    public FuncPtr(void* ptr)
    {
        _value = (long)ptr;
    }

    public void* AsPtr() => (void*)_value;
}

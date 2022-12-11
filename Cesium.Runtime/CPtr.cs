namespace Cesium.Runtime;

/// <summary>A class encapsulating an opaque pointer (aka <code>void*</code> in C).</summary>
public unsafe readonly struct CPtr
{
    private readonly long _value;

    private CPtr(long value)
    {
        _value = value;
    }

    public static implicit operator CPtr(void* ptr) => new((long)ptr);
    public void* AsPtr() => (void*)_value;
    public TResult* AsPtr<TResult>() where TResult : unmanaged => (TResult*)_value;
    public IntPtr AsIntPtr() => (IntPtr)AsPtr();
}


/// <summary>A class encapsulating an object pointer.</summary>
/// <typeparam name="T">Type this pointer may be resolved to.</typeparam>
public unsafe readonly struct CPtr<T> where T : unmanaged
{
    private readonly long _value;

    private CPtr(long value)
    {
        _value = value;
    }

    public static implicit operator CPtr<T>(T* ptr) => new((long)ptr);
    public T* AsPtr() => (T*)_value;
    public TResult* AsPtr<TResult>() where TResult : unmanaged => (TResult*)_value;
    public IntPtr AsIntPtr() => (IntPtr)AsPtr();
}

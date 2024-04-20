namespace Cesium.Runtime;

/// <summary>A class encapsulating an opaque pointer (aka <code>void*</code> in C).</summary>
public readonly unsafe struct VoidPtr
{
    private readonly IntPtr _value;

    private VoidPtr(IntPtr value)
    {
        _value = value;
    }

    public static implicit operator VoidPtr(void* ptr) => new((IntPtr)ptr);
    public void* AsPtr() => (void*)_value;
    public TResult* AsPtr<TResult>() where TResult : unmanaged => (TResult*)_value;
    public IntPtr AsIntPtr() => (IntPtr)AsPtr();
}


/// <summary>A class encapsulating an object pointer.</summary>
/// <typeparam name="T">Type this pointer may be resolved to.</typeparam>
public readonly unsafe struct CPtr<T> where T : unmanaged
{
    private readonly IntPtr _value;

    private CPtr(IntPtr value)
    {
        _value = value;
    }

    public static implicit operator CPtr<T>(T* ptr) => new((IntPtr)ptr);
    public T* AsPtr() => (T*)_value;
    public TResult* AsPtr<TResult>() where TResult : unmanaged => (TResult*)_value;
    public IntPtr AsIntPtr() => (IntPtr)AsPtr();
}

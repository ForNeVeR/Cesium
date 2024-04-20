using System.Runtime.InteropServices;

namespace Cesium.Runtime;

/// <summary>A class encapsulating a C function pointer.</summary>
public readonly unsafe struct FuncPtr<TDelegate> where TDelegate : MulticastDelegate // TODO[#487]: Think about vararg and empty parameter list encoding.
{
    private readonly IntPtr _value;

    public FuncPtr(void* ptr)
    {
        _value = (IntPtr)ptr;
    }

    public FuncPtr(IntPtr ptr)
    {
        _value = ptr;
    }

    public static implicit operator TDelegate(FuncPtr<TDelegate> funcPtr) => (TDelegate)Activator.CreateInstance(typeof(TDelegate), [null, funcPtr._value])!;
    public static implicit operator FuncPtr<TDelegate>(TDelegate @delegate) => @delegate.Method.MethodHandle.GetFunctionPointer();
    public static implicit operator FuncPtr<TDelegate>(IntPtr funcPtr) => new(funcPtr);
    public static implicit operator FuncPtr<TDelegate>(void* funcPtr) => new(funcPtr);

    public TDelegate AsDelegate() => this;

    public void* AsPtr() => (void*)_value;
}

using Cesium.Runtime.Attributes;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cesium.Runtime;

/// <summary>
/// A useful wrapper over UTF8 strings.
/// </summary>
[EquivalentType(typeof(byte*))]
[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct UTF8String
{
    public readonly static UTF8String NullString = new UTF8String((byte*)0);

    private readonly long _value;

    public UTF8String(byte* text) => _value = (long)text;

    public byte* Pointer => (byte*)_value;

    public byte this[int index]
    {
        get => Pointer[index];
        set => Pointer[index] = value;
    }

    public byte this[nuint index]
    {
        get => Pointer[index];
        set => Pointer[index] = value;
    }

    /// <summary>
    /// String length
    /// </summary>
    public nuint Length
    {
        get
        {
            nuint length = 0;
            while (Pointer[length] != 0) length++;
            return length;
        }
    }

    /// <summary>
    /// String length including '\0'
    /// </summary>
    public nuint NullTerminatedLength
    {
        get
        {
            nuint length = 0;
            while (Pointer[length] != 0) length++;
            return length + 1;
        }
    }

#if !NETSTANDARD
    /// <summary>
    /// Creates a Span for the full length of the string
    /// </summary>
    public Span<byte> Span => new(Pointer, (int)Length);

    /// <summary>
    /// Creates Span(ptr, int.MaxValue)
    /// </summary>
    public Span<byte> UncheckedSpan => new(Pointer, int.MaxValue);
#endif

    /// <summary>
    /// Copies the contents of a string for its entire length to another string
    /// </summary>
    /// <param name="dest">Destination</param>
    public void CopyTo(UTF8String dest)
    {
        var len = NullTerminatedLength;

#if NETSTANDARD
        for(nuint i = 0; i < len; i++)
            dest[i] = this[i];
#else
        UncheckedSpan.Slice(0, (int)len).CopyTo(dest.UncheckedSpan);
#endif
    }

    /// <summary>
    /// Copies "count" bytes of a string to another string
    /// </summary>
    /// <param name="dest">Destination</param>
    /// <param name="count">How many bytes to copy</param>
    public void CopyTo(UTF8String dest, nuint count)
    {
        var len = Math.Min(NullTerminatedLength, count);

#if NETSTANDARD
        for (nuint i = 0; i < len; i++)
            dest[i] = this[i];
#else
        UncheckedSpan.Slice(0, (int)len).CopyTo(dest.UncheckedSpan);
#endif
    }

    /// <summary>
    /// Looks for a literal in a string
    /// </summary>
    /// <param name="ch">ASCII literal</param>
    /// <returns>Pointer to the literal</returns>
    public UTF8String FindEntry(byte ch)
    {
#if NETSTANDARD
        var len = Length;
        for (nuint i = 0; i < len; i++)
            if (this[i] == ch)
                return At(i);
        return NullString;
#else
        var index = Span.IndexOf(ch);
        if (index == -1) return NullString;
        return (byte*)Unsafe.AsPointer(ref Unsafe.AddByteOffset(ref Unsafe.AsRef<byte>(Pointer), (nuint)index));
#endif
    }

    public UTF8String At(int index) => new UTF8String(Pointer + index);
    public UTF8String At(nuint index) => new UTF8String(Pointer + index);

    public override string ToString() => new string((sbyte*)_value);

    public static bool operator !(UTF8String str) => (nuint)str._value == 0;

    public static implicit operator byte*(UTF8String str) => str.Pointer;
    public static implicit operator IntPtr(UTF8String str) => (IntPtr)str._value;

    public static implicit operator UTF8String(byte* p) => new UTF8String(p);
    public static implicit operator UTF8String(sbyte* p) => new UTF8String((byte*)p);
    public static implicit operator UTF8String(IntPtr p) => new UTF8String((byte*)p);
    public static implicit operator UTF8String(CPtr<byte> p) => new UTF8String(p.AsPtr());
    public static implicit operator UTF8String(CPtr<sbyte> p) => new UTF8String((byte*)p.AsPtr());
}

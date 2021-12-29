namespace Cesium.Runtime;

using System.Runtime.InteropServices;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public unsafe static class StringFunctions
{
    public static uint StrLen(byte* str)
    {
        return (uint)(Marshal.PtrToStringUTF8((nint)str)?.Length ?? 0);
    }
}

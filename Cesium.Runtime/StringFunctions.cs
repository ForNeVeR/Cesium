namespace Cesium.Runtime;

using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public unsafe static class StringFunctions
{
    public static uint StrLen(byte* str)
    {
#if NETSTANDARD
        Encoding encoding = Encoding.UTF8;
        int byteLength = 0;
        byte* search = str;
        while (*search != '\0')
        {
            byteLength++;
            search++;
        }

        int stringLength = encoding.GetCharCount(str, byteLength);
        return (uint)stringLength;
#else
        return (uint)(Marshal.PtrToStringUTF8((nint)str)?.Length ?? 0);
#endif
    }
}

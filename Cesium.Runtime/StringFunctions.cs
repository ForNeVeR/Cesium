#if NETSTANDARD
using System.Text;
#endif
#if NET6_0
using System.Runtime.InteropServices;
#endif

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public unsafe static class StringFunctions
{
    public static uint StrLen(CPtr<byte> str)
    {
#if NETSTANDARD
        Encoding encoding = Encoding.UTF8;
        int byteLength = 0;
        byte* search = str.AsPtr();
        while (*search != '\0')
        {
            byteLength++;
            search++;
        }

        int stringLength = encoding.GetCharCount(str.AsPtr(), byteLength);
        return (uint)stringLength;
#else
        return (uint)(Marshal.PtrToStringUTF8(str.AsIntPtr())?.Length ?? 0);
#endif
    }
}

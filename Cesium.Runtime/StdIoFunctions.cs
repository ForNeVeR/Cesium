namespace Cesium.Runtime;

using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    public static int PutS(byte* str)
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
        string s = new string('\0', stringLength);
        fixed (char* pTempChars = s)
        {
            encoding.GetChars(str, byteLength, pTempChars, stringLength);
        }

        Console.Write(s);
#else
        Console.Write(Marshal.PtrToStringUTF8((nint)str));
#endif
        return 1;
    }
}

using System.Runtime.InteropServices;
using System.Text;

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    public static void PutS(byte* str)
    {
        try
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
            // return 0; // TODO[#156]: Uncomment
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            const int EOF = -1; // TODO[#155]: Extract to some common place.
            // return EOF; // TODO[#156]: Uncomment
        }
    }
}

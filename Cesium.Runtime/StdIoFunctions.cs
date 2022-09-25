using System.Runtime.InteropServices;
#if NETSTANDARD
using System.Text;
#endif

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
            Console.Write(Unmarshal(str));
            // return 0; // TODO[#156]: Uncomment
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            const int EOF = -1; // TODO[#155]: Extract to some common place.
            // return EOF; // TODO[#156]: Uncomment
        }
    }

    public static void PrintF(byte* str, params object[] varargs)
    {
        var formatString = Unmarshal(str);
        if (formatString == null)
        {
            return;
        }

        int currentPosition = 0;
        var formatStartPosition = formatString.IndexOf('%', currentPosition);
        int consumedArgs = 0;
        while (formatStartPosition >= 0)
        {
            Console.Write(formatString.Substring(currentPosition, formatStartPosition - currentPosition));
            var formatSpecifier = formatString[formatStartPosition + 1];
            switch (formatSpecifier)
            {
                case 's':
                    Console.Write(Unmarshal((byte*)(IntPtr)varargs[consumedArgs]));
                    consumedArgs++;
                    break;
                default:
                    throw new FormatException($"Format specifier {formatSpecifier} is not supported");
            }

            currentPosition = formatStartPosition + 2;
            formatStartPosition = formatString.IndexOf('%', currentPosition);
        }

        Console.Write(formatString.Substring(currentPosition));
    }

    private static string? Unmarshal(byte* str)
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

        return s;
#else
        return Marshal.PtrToStringUTF8((nint)str);
#endif
    }
}

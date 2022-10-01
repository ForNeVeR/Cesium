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
#pragma warning disable CS0219
            const int EOF = -1; // TODO[#155]: Extract to some common place.
#pragma warning restore CS0219
            // return EOF; // TODO[#156]: Uncomment
        }
    }

    public static void PrintF(byte* str, void* varargs)
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
            int addition = 1;
            string formatSpecifier = formatString[formatStartPosition + addition].ToString();
            if (formatString[formatStartPosition + addition] == 'l')
            {
                addition++;
                formatSpecifier += formatString[formatStartPosition + addition].ToString();
            }

            switch (formatSpecifier)
            {
                case "s":
                    Console.Write(Unmarshal((byte*)((long*)varargs)[consumedArgs]));
                    consumedArgs++;
                    break;
                case "c":
                    Console.Write((char)(byte)((long*)varargs)[consumedArgs]);
                    consumedArgs++;
                    break;
                case "d":
                    Console.Write((int)((long*)varargs)[consumedArgs]);
                    consumedArgs++;
                    break;
                case "u":
                case "lu":
                    Console.Write((uint)((long*)varargs)[consumedArgs]);
                    consumedArgs++;
                    break;
                case "f":
                    var floatNumber = ((double*)varargs)[consumedArgs];
                    Console.Write(floatNumber.ToString("F6"));
                    consumedArgs++;
                    break;
                case "p":
                    nint pointerValue = ((nint*)varargs)[consumedArgs];
                    Console.Write(pointerValue.ToString("X"));
                    consumedArgs++;
                    break;
                default:
                    throw new FormatException($"Format specifier {formatSpecifier} is not supported");
            }

            currentPosition = formatStartPosition + addition + 1;
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

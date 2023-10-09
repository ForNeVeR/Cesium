using System.IO;
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
    record class StreamHandle
    {
        public required string FileMode { get; set; }
        public Func<TextReader>? Reader { get; set; }
        public Func<TextWriter>? Writer { get; set; }
    }

    private static List<StreamHandle> handles = new();

    private const int StdIn = 0;

    private const int StdOut = 1;

    private const int StdErr = 2;

    static StdIoFunctions()
    {
        handles.Add(new StreamHandle()
        {
            FileMode = "r",
            Reader = () => Console.In,
        });
        handles.Add(new StreamHandle()
        {
            FileMode = "w",
            Writer = () => Console.Out,
        });
        handles.Add(new StreamHandle()
        {
            FileMode = "w",
            Writer = () => Console.Error,
        });
    }

    public static int PutS(byte* str)
    {
        try
        {
            Console.WriteLine(Unmarshal(str));
            return 0;
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            const int EOF = -1; // TODO[#155]: Extract to some common place.
            return EOF;
        }
    }
    public static int PutChar(byte character)
    {
        try
        {
            Console.Write((char)character);
            return character;
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            const int EOF = -1; // TODO[#155]: Extract to some common place.
            return EOF;
        }
    }

    public static int PutC(byte character, void* stream)
    {
        try
        {
            var streamDescriptor = GetStreamHandle(stream);
            if (streamDescriptor == null)
            {
                return -1;
            }

            streamDescriptor.Writer!().Write((char)character);
            return character;
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            const int EOF = -1; // TODO[#155]: Extract to some common place.
            return EOF;
        }
    }

    public static int PrintF(byte* str, void* varargs)
    {
        return FPrintF((void*)(IntPtr)StdOut, str, varargs);
    }

    public static int FPrintF(void* stream, byte* str, void* varargs)
    {
        var formatString = Unmarshal(str);
        if (formatString == null)
        {
            return -1;
        }

        var streamHandle = GetStreamHandle(stream);
        if (streamHandle == null)
        {
            return -1;
        }

        var streamWriterAccessor = streamHandle.Writer;
        if (streamWriterAccessor == null)
        {
            return -1;
        }

        var streamWriter = streamWriterAccessor();

        int currentPosition = 0;
        var formatStartPosition = formatString.IndexOf('%', currentPosition);
        int consumedArgs = 0;
        int consumedBytes = 0;
        while (formatStartPosition >= 0)
        {
            var lengthTillPercent = formatStartPosition - currentPosition;
            streamWriter.Write(formatString.Substring(currentPosition, lengthTillPercent));
            consumedBytes += lengthTillPercent;
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
                    string? stringValue = Unmarshal((byte*)((long*)varargs)[consumedArgs]);
                    streamWriter.Write(stringValue);
                    consumedBytes += stringValue?.Length ?? 0;
                    consumedArgs++;
                    break;
                case "c":
                    streamWriter.Write((char)(byte)((long*)varargs)[consumedArgs]);
                    consumedBytes++;
                    consumedArgs++;
                    break;
                case "d":
                case "li":
                case "i":
                    int intValue = (int)((long*)varargs)[consumedArgs];
                    var intValueString = intValue.ToString();
                    streamWriter.Write(intValueString);
                    consumedBytes += intValueString.Length;
                    consumedArgs++;
                    break;
                case "u":
                case "lu":
                    uint uintValue = (uint)((long*)varargs)[consumedArgs];
                    var uintValueString = uintValue.ToString();
                    streamWriter.Write(uintValueString);
                    consumedBytes += uintValueString.Length;
                    consumedArgs++;
                    break;
                case "f":
                    var floatNumber = ((double*)varargs)[consumedArgs];
                    string floatNumberString = floatNumber.ToString("F6");
                    streamWriter.Write(floatNumberString);
                    consumedBytes += floatNumberString.Length;
                    consumedArgs++;
                    break;
                case "p":
                    nint pointerValue = ((nint*)varargs)[consumedArgs];
                    string pointerValueString = pointerValue.ToString("X");
                    streamWriter.Write(pointerValueString);
                    consumedBytes += pointerValueString.Length;
                    consumedArgs++;
                    break;
                case "%":
                    streamWriter.Write('%');
                    consumedBytes += 1;
                    break;
                default:
                    throw new FormatException($"Format specifier {formatSpecifier} is not supported");
            }

            currentPosition = formatStartPosition + addition + 1;
            formatStartPosition = formatString.IndexOf('%', currentPosition);
        }

        string remainderString = formatString.Substring(currentPosition);
        streamWriter.Write(remainderString);
        return consumedBytes + remainderString.Length;
    }

    internal static string? Unmarshal(byte* str)
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

    private static StreamHandle? GetStreamHandle(void* stream)
    {
        var handleIndex = (int)(IntPtr)stream;
        var result = handles.ElementAtOrDefault(handleIndex);
        return result;
    }
}

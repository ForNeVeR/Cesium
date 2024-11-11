using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#if !NETSTANDARD
using System.Runtime.InteropServices;
#endif

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    internal record StreamHandle
    {
        public required string FileMode { get; set; }
        public FileStream? Stream { get; set; }
        public Func<TextReader>? Reader { get; set; }
        public Func<TextWriter>? Writer { get; set; }
        public int ErrNo { get; set; }
    }

    private static object _locker = new();
    internal static readonly List<StreamHandle> Handles = [];

    private const int StdIn = 0;

    private const int StdOut = 1;

    private const int StdErr = 2;

    static StdIoFunctions()
    {
        Handles.Add(new StreamHandle()
        {
            FileMode = "r",
            Reader = () => Console.In,
        });
        Handles.Add(new StreamHandle()
        {
            FileMode = "w",
            Writer = () => Console.Out,
        });
        Handles.Add(new StreamHandle()
        {
            FileMode = "w",
            Writer = () => Console.Error,
        });
    }

    public static int PutS(byte* str)
    {
        try
        {
            Console.WriteLine(RuntimeHelpers.Unmarshal(str));
            return 0;
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            const int EOF = -1; // TODO[#155]: Extract to some common place.
            return EOF;
        }
    }

    public static int GetChar()
    {
        return FGetC((void*)(IntPtr)StdIn);
    }

    public static int FPutS(byte* str, void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
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

        try
        {
            streamWriter.Write(RuntimeHelpers.Unmarshal(str));
            streamWriter.Flush();
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
            var streamDescriptor = GetStream((IntPtr)stream);
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
        var formatString = RuntimeHelpers.Unmarshal(str);
        if (formatString == null)
        {
            return -1;
        }

        var streamHandle = GetStream((IntPtr)stream);
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
            int width = 0;
            bool alwaysSign = false;
            if (formatString[formatStartPosition + addition] == '+')
            {
                alwaysSign = true;
                addition++;
            }

            bool leftAdjust = false;
            if (formatString[formatStartPosition + addition] == '-')
            {
                leftAdjust = true;
                addition++;
            }

            bool zeroPrepend = false;
            if (formatString[formatStartPosition + addition] == '0')
            {
                zeroPrepend = true;
                addition++;
            }

            bool alternativeImplementation = false;
            if (formatString[formatStartPosition + addition] == '#')
            {
                alternativeImplementation = true;
                addition++;
            }

            if (formatString[formatStartPosition + addition] == '0')
            {
                addition++;
            }

            while (formatString[formatStartPosition + addition] >= '0' && formatString[formatStartPosition + addition] <= '9')
            {
                width = width * 10 + (formatString[formatStartPosition + addition] - '0');
                addition++;
            }

            int paddingRequested = 0; // 0 - not set, -1 - star
            if (formatString[formatStartPosition + addition] == '*')
            {
                paddingRequested = -1;
                addition++;
            }

            int precision = -1; // -1 - not set, -2 - star
            if (formatString[formatStartPosition + addition] == '.')
            {
                addition++;
                if (formatString[formatStartPosition + addition] == '*')
                {
                    precision = -2;
                    addition++;

                    while (formatString[formatStartPosition + addition] >= '0' && formatString[formatStartPosition + addition] <= '9')
                    {
                        if (precision == -2) precision = 0;
                        precision = precision * 10 + (formatString[formatStartPosition + addition] - '0');
                        addition++;
                    }
                }
                else
                {
                    while (formatString[formatStartPosition + addition] >= '0' && formatString[formatStartPosition + addition] <= '9')
                    {
                        if (precision == -1) precision = 0;
                        precision = precision * 10 + (formatString[formatStartPosition + addition] - '0');
                        addition++;
                    }
                }
            }

            string formatSpecifier = formatString[formatStartPosition + addition].ToString();
            if (formatString[formatStartPosition + addition] == 'l' || formatString[formatStartPosition + addition] == 'z')
            {
                addition++;
                formatSpecifier += formatString[formatStartPosition + addition].ToString();
            }

            int padding = -1;
            if (paddingRequested == -1)
            {
                padding = (int)((long*)varargs)[consumedArgs];
                consumedArgs++;
            }
            else if (width != 0)
            {
                padding = width;
            }

            int trim = -1;
            if (precision == -2)
            {
                trim = (int)((long*)varargs)[consumedArgs];
                consumedArgs++;
            }
            else if (precision >= 0)
            {
                trim = precision;
            }

            switch (formatSpecifier)
            {
                case "s":
                    {
                        string? stringValue = RuntimeHelpers.Unmarshal((byte*)((long*)varargs)[consumedArgs]);
                        if (trim != -1)
                        {
                            stringValue = stringValue?.Substring(0, Math.Max(0, Math.Min(stringValue.Length - 1, trim)));
                        }

                        if (padding != -1)
                        {
                            if (leftAdjust)
                            {
                                var actualLength = stringValue?.Length ?? 0;
                                if (actualLength < padding)
                                {
                                    stringValue += new string(' ', padding - actualLength);
                                }
                            }
                            else
                            {
                                stringValue = string.Format("{0," + padding + "}", stringValue);
                            }
                        }

                        streamWriter.Write(stringValue);
                        consumedBytes += stringValue?.Length ?? 0;
                        consumedArgs++;
                        break;
                    }
                case "c":
                    streamWriter.Write((char)(byte)((long*)varargs)[consumedArgs]);
                    consumedBytes++;
                    consumedArgs++;
                    break;
                case "d":
                case "ld":
                case "i":
                    int intValue = (int)((long*)varargs)[consumedArgs];
                    var intValueString = intValue.ToString();
                    if (alwaysSign && intValue > 0)
                    {
                        streamWriter.Write('+');
                    }

                    if (intValueString.Length < precision)
                    {
                        streamWriter.Write(new string('0', precision - intValueString.Length));
                    }

                    if (precision != 0 || intValue != 0)
                    {
                        streamWriter.Write(intValueString);
                    }

                    consumedBytes += intValueString.Length;
                    consumedArgs++;
                    break;
                case "li":
                    long longValue = ((long*)varargs)[consumedArgs];
                    var longValueString = longValue.ToString();
                    if (alwaysSign && longValue > 0)
                    {
                        streamWriter.Write('+');
                    }

                    if (longValueString.Length < precision)
                    {
                        streamWriter.Write(new string('0', precision - longValueString.Length));
                    }

                    if (precision != 0 || longValue != 0)
                    {
                        streamWriter.Write(longValueString);
                    }

                    consumedBytes += longValueString.Length;
                    consumedArgs++;
                    break;
                case "u":
                {
                    uint uintValue = (uint)((long*)varargs)[consumedArgs];
                    var uintValueString = uintValue.ToString();
                    streamWriter.Write(uintValueString);
                    consumedBytes += uintValueString.Length;
                    consumedArgs++;
                    break;
                }
                case "lu":
                case "zu":
                    {
                    ulong ulongValue = (ulong)((long*)varargs)[consumedArgs];
                    var ulongValueString = ulongValue.ToString();
                    streamWriter.Write(ulongValueString);
                    consumedBytes += ulongValueString.Length;
                    consumedArgs++;
                    break;
                }
                case "f":
                    {
                        var floatNumber = ((double*)varargs)[consumedArgs];
                        string floatNumberString = floatNumber.ToString("F" + (trim == -1 ? 6 : trim));
                        if (alwaysSign && floatNumber > 0)
                        {
                            streamWriter.Write('+');
                        }

                        if (floatNumberString.Length < width)
                        {
                            streamWriter.Write(new string(zeroPrepend ? '0' : ' ', width - floatNumberString.Length));
                        }

                        streamWriter.Write(floatNumberString);
                        consumedBytes += floatNumberString.Length;
                        consumedArgs++;
                        break;
                    }
                case "e":
                case "E":
                    {
                        var floatNumber = ((double*)varargs)[consumedArgs];
                        //streamWriter.Write($"!padding {padding} trim {trim} precision {precision} ");
                        string floatNumberString = floatNumber.ToString("0." + new string('0', trim == -1 ? 6 : trim) + formatSpecifier + "+00");
                        if (alwaysSign && floatNumber > 0)
                        {
                            streamWriter.Write('+');
                        }

                        streamWriter.Write(floatNumberString);
                        //streamWriter.Write($"!");
                        consumedBytes += floatNumberString.Length;
                        consumedArgs++;
                        break;
                    }
                case "o":
                    {
                        uint uintValue = (uint)((long*)varargs)[consumedArgs];
                        StringBuilder stringBuilder = new();
                        while (uintValue >= 8)
                        {
                            stringBuilder.Insert(0, (uintValue % 8));
                            uintValue /= 8;
                        }

                        stringBuilder.Insert(0, uintValue);

                        var stringValue = stringBuilder.ToString();
                        if (paddingRequested == -1)
                        {
                            stringValue = string.Format("{0," + padding + "}", stringValue);
                        }

                        if (alternativeImplementation && stringValue[0] != '0')
                        {
                            streamWriter.Write('0');
                        }

                        streamWriter.Write(stringValue);
                        consumedBytes += stringValue.Length;
                        consumedArgs++;
                        break;
                    }
                case "p":
                    nint pointerValue = ((nint*)varargs)[consumedArgs];
                    string pointerValueString = pointerValue.ToString("X");
                    streamWriter.Write(pointerValueString);
                    consumedBytes += pointerValueString.Length;
                    consumedArgs++;
                    break;
                case "x":
                case "X":
                    nuint hexadecimalValue = ((nuint*)varargs)[consumedArgs];
                    if (hexadecimalValue != 0)
                    {
                        if (alternativeImplementation)
                        {
                            streamWriter.Write('0');
                            streamWriter.Write(formatSpecifier);
                        }

                        var targetFormat = "{0:" + formatSpecifier + (width == 0 ? "" : width) + "}";
                        // NOTE: without converting nuint to long, this was broken on .NET Framework
                        var hexadecimalValueString = string.Format(targetFormat, (long)hexadecimalValue);
                        streamWriter.Write(hexadecimalValueString);
                        consumedBytes += hexadecimalValueString.Length;
                        consumedArgs++;
                    }
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

    public static unsafe void* FOpen(byte* filename, byte* mode)
    {
        void* streamptr;
        var errorCode = FOpenS(&streamptr, filename, mode);
        if (errorCode != 0)
        {
            StdLibFunctions.SetErrNo(errorCode);
            return null;
        }

        return streamptr;
    }

    public static int FOpenS(void** streamptr, byte* filename, byte* mode)
    {
        if (filename == null) return ErrNo.EACCES;
        var fileName = CesiumFunctions.Unmarshal(filename);
        if (fileName == null) return ErrNo.EINVAL;
        var modeString = CesiumFunctions.Unmarshal(mode);
        if (modeString == null) return ErrNo.EINVAL;
        FileMode fileMode;
        FileAccess fileAccess;
        var extended = modeString.Contains('+');
        if (modeString[0] == 'r')
        {
            fileMode = FileMode.Open;
            fileAccess = !extended ? FileAccess.Read : FileAccess.ReadWrite;
        }
        else if (modeString[0] == 'w')
        {
            fileMode = modeString.Contains('x') ? FileMode.CreateNew : FileMode.Create;
            fileAccess = !extended ? FileAccess.Write : FileAccess.ReadWrite;
        }
        else if (modeString[0] == 'a')
        {
            fileMode = FileMode.OpenOrCreate;
            fileAccess = !extended ? FileAccess.Write : FileAccess.ReadWrite;
        }
        else
        {
            return ErrNo.EINVAL;
        }

        FileStream stream;
        stream = new(fileName, fileMode, fileAccess);
        var handle = new StreamHandle()
        {
            FileMode = modeString,
            Stream = stream,
        };
        if (fileAccess != FileAccess.Write)
        {
            handle.Reader = () => new StreamReader(stream);
        }
        if (fileAccess != FileAccess.Read)
        {
            handle.Writer = () => new StreamWriter(stream);
        }

        *streamptr = (void*)AddStream(handle);
        return 0;
    }

    public static int FClose(void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        streamHandle.Stream!.Close();
        return RemoveStream((IntPtr)stream) ? 0 : ErrNo.EBADF;
    }

    public static int FGetC(void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        return GetCharacterFromFile(streamHandle);
    }

    private static int GetCharacterFromFile(StreamHandle streamHandle)
    {
        if (streamHandle.Stream is null)
        {
            var reader = streamHandle.Reader;
            if (reader is null)
            {
                return ErrNo.EBADF;
            }

            return reader().Read();
        }

        return streamHandle.Stream.ReadByte();
    }

    public static byte* FGetS(byte* str, int count, void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return null;
        }

        byte[] buffer = new byte[count];
        for (var i = 0; i < count - 1; i++)
        {
            var result = GetCharacterFromFile(streamHandle);
            if (result == -1)
            {
                str[i] = 0;
                return null;
            }

            str[i] = (byte)result;
            if (result == '\n')
            {
                str[i+1] = 0;
                break;
            }
        }

        str[count - 1] = 0;
        return str;
    }

    public static int FEof(void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        return streamHandle.Reader!().Peek() == -1 ? 1 : 0;
    }

    public static int FSeek(void* stream, long offset, int origin)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        streamHandle.Stream!.Seek(offset, (SeekOrigin)origin);
        return 0;
    }

    public static int FError(void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        return streamHandle.ErrNo;
    }

    public static int Rewind(void* stream)
    {
        var streamHandle = GetStream((IntPtr)stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        streamHandle.Stream!.Flush(true);
        streamHandle.Stream!.Seek(0, SeekOrigin.Begin);
        return 0;
    }

    public static void PError(byte* s)
    {
        Console.Error.WriteLine($"{CesiumFunctions.Unmarshal(s)}: {StringFunctions.StrError(*StdLibFunctions.GetErrNo())}");
    }

    public static int Remove(byte* pathname)
    {
        File.Delete(CesiumFunctions.Unmarshal(pathname)!);
        return 0;
    }

    internal static StreamHandle? GetStream(IntPtr handle)
    {
        var handleValue = handle.ToInt64();
        if (handleValue is StdIn or StdOut or StdErr)
        {
            return Handles[(int)handleValue];
        }

        try
        {
            lock (_locker)
            {
                var gchAddr = Marshal.ReadIntPtr(handle);
                var gch = GCHandle.FromIntPtr(gchAddr);

                return gch.Target as StreamHandle;
            }
        }
        catch (AccessViolationException)
        {
            return null;
        }
    }

    internal static IntPtr AddStream(StreamHandle stream)
    {
        lock (_locker)
        {
            var gch = GCHandle.Alloc(stream);
            var handel = GCHandle.ToIntPtr(gch);

            var ptr = Marshal.AllocHGlobal(sizeof(GCHandle));
            Marshal.WriteIntPtr(ptr, handel);
            return ptr;
        }
    }

    internal static bool RemoveStream(IntPtr handle)
    {
        try
        {
            lock (_locker)
            {
                var gchAddr = Marshal.ReadIntPtr(handle);
                var gch = GCHandle.FromIntPtr(gchAddr);

                if (gch.Target is not StreamHandle) return false;

                gch.Free();
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
        catch (AccessViolationException)
        {
            return false;
        }
    }
}

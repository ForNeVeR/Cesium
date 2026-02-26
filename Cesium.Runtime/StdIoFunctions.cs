// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    internal record StreamHandle
    {
        public required string FileMode { get; set; }
        public Stream? Stream { get; set; }
        public Func<TextReader>? Reader { get; set; }
        public Func<TextWriter>? Writer { get; set; }
        public int ErrNo { get; set; }
        public Action<Stream>? CloseCallback { get; set; }
    }

    internal static readonly List<StreamHandle> Handles = [];

    private const int _stdIn = 0;

    private const int _stdOut = 1;

    private const int _stdErr = 2;

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
        return FGetC((void*)(IntPtr)_stdIn);
    }

    public static int FPutS(byte* str, void* stream)
    {
        var streamHandle = GetStream(stream);
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
            var streamDescriptor = GetStream(stream);
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
        return FPrintF((void*)_stdOut, str, varargs);
    }

    public static int FPrintF(void* stream, byte* str, void* varargs)
    {
        var formatString = RuntimeHelpers.Unmarshal(str);
        if (formatString == null)
        {
            return -1;
        }

        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            return -1;
        }

        var streamWriterAccessor = streamHandle.Writer;
        if (streamWriterAccessor == null)
        {
            return -1;
        }

        using var streamWriter = streamWriterAccessor();
        var errorCode = StreamPrintF(streamWriter, formatString, varargs);
        FFlush(streamHandle);
        return errorCode;
    }

    public static int SPrintF(byte* buffer, byte* str, void* varargs)
    {
        var formatString = RuntimeHelpers.Unmarshal(str);
        if (formatString == null)
        {
            return -1;
        }

        var streamWriter = new BytePtrTextWriter(buffer);
        return StreamPrintF(streamWriter, formatString, varargs);
    }

    private static int StreamPrintF(TextWriter streamWriter, string formatString, void* varargs)
    {
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

            while (formatString[formatStartPosition + addition].IsAsciiDigit())
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

                    while (formatString[formatStartPosition + addition].IsAsciiDigit())
                    {
                        if (precision == -2) precision = 0;
                        precision = precision * 10 + (formatString[formatStartPosition + addition] - '0');
                        addition++;
                    }
                }
                else
                {
                    while (formatString[formatStartPosition + addition].IsAsciiDigit())
                    {
                        if (precision == -1) precision = 0;
                        precision = precision * 10 + (formatString[formatStartPosition + addition] - '0');
                        addition++;
                    }
                }
            }

            string formatSpecifier = formatString[formatStartPosition + addition].ToString();
            if (char.ToLowerInvariant(formatString[formatStartPosition + addition]) == 'l'
                || char.ToLowerInvariant(formatString[formatStartPosition + addition]) == 'z'
                || char.ToLowerInvariant(formatString[formatStartPosition + addition]) == 'u')
            {
                if (formatStartPosition + addition < formatString.Length - 1
                    && (char.ToLowerInvariant(formatSpecifier[0]) != 'u' || char.ToLowerInvariant(formatString[formatStartPosition + addition + 1]) == 'l'))
                {
                    addition++;
                    formatSpecifier += formatString[formatStartPosition + addition].ToString();
                    if (string.Equals(formatSpecifier, "ll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        addition++;
                        formatSpecifier += formatString[formatStartPosition + addition].ToString();
                    }

                    if (string.Equals(formatSpecifier, "ul", StringComparison.InvariantCultureIgnoreCase) && char.ToLowerInvariant(formatString[formatStartPosition + addition]) == 'l')
                    {
                        addition++;
                        formatSpecifier += formatString[formatStartPosition + addition].ToString();
                    }
                }
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

            bool trimZero = false;
            if (formatSpecifier == "g" || formatSpecifier == "G")
            {
                trimZero = true;
                var floatNumber = ((double*)varargs)[consumedArgs];
                var exponent = (int)Math.Log10(floatNumber);
                if (precision == -1)
                {
                    precision = 6;
                }
                if (precision > exponent && exponent >= -4)
                {
                    formatSpecifier = formatSpecifier == "g" ? "f" : "F";
                    precision = precision - 1 - exponent;
                }
                else
                {
                    formatSpecifier = formatSpecifier == "g" ? "e" : "E";
                    precision = precision - 1;
                }
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
                case "lld":
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
                case "llu":
                case "LLu":
                case "LLU":
                case "ull":
                case "Ull":
                case "ULL":
                case "lu":
                case "Lu":
                case "LU":
                case "UL":
                case "Ul":
                case "ul":
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
                        string floatNumberString = floatNumber.ToString("F" + (trim == -1 ? 6 : trim), CultureInfo.InvariantCulture.NumberFormat);
                        if (alwaysSign && floatNumber > 0)
                        {
                            streamWriter.Write('+');
                        }

                        if (floatNumberString.Length < width)
                        {
                            streamWriter.Write(new string(zeroPrepend ? '0' : ' ', width - floatNumberString.Length));
                        }

                        if (trimZero)
                        {
                            floatNumberString = floatNumberString.TrimEnd('0');
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
                        string floatNumberString = floatNumber.ToString("0." + new string(trimZero ? '#' : '0', trim == -1 ? 6 : trim) + formatSpecifier + "+00", CultureInfo.InvariantCulture.NumberFormat);
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
            handle.Writer = () => new StreamWriter(stream, Encoding.UTF8, 8192, true);
        }

        *streamptr = AddStream(handle);
        return 0;
    }

    public static int FClose(void* stream)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        FFlush(streamHandle);
        streamHandle.Stream!.Close();
        return FreeStream(stream) ? 0 : ErrNo.EBADF;
    }

    public static int FGetC(void* stream)
    {
        var streamHandle = GetStream(stream);
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
        var streamHandle = GetStream(stream);
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
        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        if (streamHandle.Reader is null)
        {
            return 0;
        }

        return streamHandle.Reader!().Peek() == -1 ? 1 : 0;
    }

    public static int FSeek(void* stream, long offset, int origin)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        streamHandle.Stream!.Seek(offset, (SeekOrigin)origin);
        return 0;
    }

    public static int FError(void* stream)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        return streamHandle.ErrNo;
    }

    public static int Rewind(void* stream)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            return ErrNo.EBADF;
        }

        FFlush(streamHandle);
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

    public static int ScanF(byte* format, void* varargs)
    {
        return FScanF((void*)_stdIn, format, varargs);
    }

    public static int FScanF(void* stream, byte* format, void* varargs)
    {
        var formatString = RuntimeHelpers.Unmarshal(format);
        if (formatString is null)
            return -1;

        var streamHandle = GetStream(stream);

        var getStreamReader = streamHandle?.Reader;
        if (getStreamReader is null)
            return -1;

        var streamReader = getStreamReader();

        int argsConsumed = 0;
        bool whitespacePrefix = false;

        int specifierPosition = formatString.IndexOf("%", 0, StringComparison.Ordinal);

        if (specifierPosition > 0 &&
            char.IsWhiteSpace(formatString[specifierPosition - 1]))
        {
            whitespacePrefix = true;
        }

        while (specifierPosition >= 0 && specifierPosition < formatString.Length)
        {
            if (char.IsWhiteSpace(formatString[specifierPosition]))
            {
                whitespacePrefix = true;
                specifierPosition++;
                continue;
            }
            if (formatString[specifierPosition] != '%') break;

            int offset = 1;
            bool ignore = false;
            long width = -1;

            if (formatString[specifierPosition + offset] == '*')
            {
                ignore = true;
                offset++;
            }

            while (formatString[specifierPosition + offset].IsAsciiDigit())
            {
                if (width == -1) width = 0;

                width = width * 10 + (formatString[specifierPosition + offset] - '0');
                offset++;
            }

            bool isConsumed = false;

            if (formatString[specifierPosition + offset] == '[')
            {
                // TODO: Add set support
                throw new FormatException("Sets are not supported in current version");
            }

            var sb = new StringBuilder();

            if (formatString[specifierPosition + offset] == 'l')
            {
                sb.Append(formatString[specifierPosition + offset]);
                offset++;
            }
            if (formatString[specifierPosition + offset].IsSizeAttribute())
            {
                sb.Append(formatString[specifierPosition + offset]);
                offset++;
            }

            sb.Append(formatString[specifierPosition + offset]);

            string formatSpecifier = sb.ToString();
            int charsConsumed = 0;
            switch (formatSpecifier)
            {
                case "c":
                case "lc":
                    if (width == -1) width = 1;

                    var charPtr = (byte*)((long*)varargs)[argsConsumed];

                    if (whitespacePrefix)
                    {
                        while (char.IsWhiteSpace((char)streamReader.Peek()))
                        {
                            streamReader.Read();
                        }
                    }

                    while (charsConsumed < width)
                    {
                        char c = (char)streamReader.Read();

                        if (!ignore) charPtr[charsConsumed] = (byte)c;
                        charsConsumed++;
                    }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "s":
                case "ls":
                    var stringPtr = (byte*)((long*)varargs)[argsConsumed];

                    while (char.IsWhiteSpace((char)streamReader.Peek()))
                    {
                        streamReader.Read();
                    }

                    while (charsConsumed < width || width == -1)
                    {
                        if (streamReader.Peek() == -1 &&
                            charsConsumed == 0 &&
                            argsConsumed == 0) return -1;

                        if (streamReader.Peek() == -1 ||
                            char.IsWhiteSpace((char)streamReader.Peek())) break;

                        char c = (char)streamReader.Read();
                        if (!ignore) stringPtr[charsConsumed] = (byte)c;
                        charsConsumed++;
                    }

                    stringPtr[charsConsumed] = 0;

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "d":
                case "i":
                    var intPtr = (int*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numInt = (int)ParseInteger(10);
                        if (!ignore) *intPtr = numInt;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "ld":
                case "li":
                    var longPtr = (nint*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numLong = (nint)ParseInteger(10);
                        if (!ignore) *longPtr = numLong;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "lld":
                case "lli":
                    var longLongPtr = (long*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numLongLong = (long)ParseInteger(10);
                        if (!ignore) *longLongPtr = numLongLong;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "hd":
                case "hi":
                    var shortPtr = (short*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numShort = (short)ParseInteger(10);
                        if (!ignore) *shortPtr = numShort;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "u":
                    var uintPtr = (uint*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numUInt = (uint)ParseInteger(10);
                        if (!ignore) *uintPtr = numUInt;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "lu":
                    var uLongPtr = (nuint*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numULong = (nuint)ParseInteger(10);
                        if (!ignore) *uLongPtr = numULong;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "llu":
                    var uLongLongPtr = (ulong*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numULongLong = ParseInteger(10);
                        if (!ignore) *uLongLongPtr = numULongLong;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "hu":
                    var uShortPtr = (ushort*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var numUShort = (ushort)ParseInteger(10);
                        if (!ignore) *uShortPtr = numUShort;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;
                    break;

                case "f":
                case "F":
                case "e":
                case "E":
                case "a":
                case "A":
                case "g":
                case "G":
                    var floatPtr = (float*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var floatNum = ParseFloat();
                        if (!ignore) *floatPtr = floatNum;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "o":
                    var octalPtr = (int*)((long*)varargs)[argsConsumed];

                    try
                    {
                        var num = (int)ParseInteger(8);
                        if (!ignore) *octalPtr = num;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "x":
                case "X":
                case "p":
                    var hexPtr = (int*)((long*)varargs)[argsConsumed];

                    while (char.IsWhiteSpace((char)streamReader.Peek()))
                    {
                        streamReader.Read();
                    }

                    try
                    {
                        if (formatSpecifier.ToLower() == "p")
                        {
                            if ((char)streamReader.Peek() == '0')
                                streamReader.Read();
                            if ((char)streamReader.Peek() is 'x' or 'X')
                                streamReader.Read();
                            if (!((char)streamReader.Peek()).IsHexDigit())
                                throw new FormatException();
                        }

                        var num = (int)ParseInteger(16);
                        if (!ignore) *hexPtr = num;
                    }
                    catch (EndOfStreamException) { return -1; }
                    catch (FormatException) { return argsConsumed; }

                    isConsumed = !ignore;
                    offset++;

                    break;

                case "n":
                    var consumedPtr = (int*)((long*)varargs)[argsConsumed];
                    *consumedPtr = argsConsumed;

                    offset++;
                    break;

                default:
                    throw new FormatException($"Format specifier {formatSpecifier} is not supported");
            }

            if (isConsumed) argsConsumed++;
            else if (!ignore) break;

            specifierPosition += offset;
            whitespacePrefix = false;

            ulong ParseInteger(int radix)
            {
                while (char.IsWhiteSpace((char)streamReader.Peek()))
                {
                    streamReader.Read();
                }

                char first = (char)streamReader.Peek();

                bool isNegative = false;

                if (first is '+' or '-')
                {
                    isNegative = first == '-';
                    streamReader.Read();
                }

                if (!((char)streamReader.Peek()).IsAsciiDigit() && radix <= 10)
                    throw new FormatException($"No digit symbol");
                if (!((char)streamReader.Peek()).IsHexDigit())
                    throw new FormatException($"No digit symbol");

                ulong num = 0;
                while (charsConsumed < width || width == -1)
                {
                    if (streamReader.Peek() == -1 &&
                        charsConsumed == 0 &&
                        argsConsumed == 0) throw new EndOfStreamException();

                    if (radix == 8)
                    {
                        if (streamReader.Peek() == -1 ||
                            !((char)streamReader.Peek()).IsOctalDigit()) break;
                    }
                    else if (radix == 10)
                    {
                        if (streamReader.Peek() == -1 ||
                            !((char)streamReader.Peek()).IsAsciiDigit()) break;
                    }
                    else if (radix == 16)
                    {
                        if (streamReader.Peek() == -1 ||
                            !((char)streamReader.Peek()).IsHexDigit()) break;
                    }
                    else throw new FormatException($"Radix {radix} is not supported");

                    num = num * (ulong)radix + (ulong)((char)streamReader.Read()).Num();
                    charsConsumed++;
                }

                if (charsConsumed == 0) throw new FormatException("Invalid integer");

                return isNegative ? (ulong)((long)num * -1) : num;
            }

            float ParseFloat()
            {
                var integer = (long)ParseInteger(10);
                if ((char)streamReader.Peek() != '.')
                    return integer;

                streamReader.Read();
                int next = streamReader.Peek();
                if (char.IsWhiteSpace((char)next) || next == -1) return integer;
                if (!((char)next).IsAsciiDigit()) throw new FormatException();

                var fraction = (long)ParseInteger(10);

                var exponent = 0L;
                if ((char)streamReader.Peek() is 'e' or 'E')
                {
                    streamReader.Read();
                    exponent = (long)ParseInteger(10);
                }

                return float.Parse($"{integer}.{fraction}E{exponent}", CultureInfo.InvariantCulture);
            }
        }

        return argsConsumed;
    }

    public static void* OpenMemStream(byte** ptr, nuint* sizeloc)
    {
        var memoryStream = new AutoGrowStream(ptr, sizeloc);
        var streamWriter = new StreamWriter(memoryStream);
        var handle = new StreamHandle()
        {
            FileMode = "w",
            Stream = memoryStream,
            Writer = () => streamWriter,
        };
        var streamptr = AddStream(handle);
        *ptr = null;
        *sizeloc = 0;
        return streamptr;
    }

    public static nuint FRead(void* buffer, nuint size, nuint count, void* stream)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle is null || streamHandle.Reader is null)
        {
            StdLibFunctions.SetErrNo(ErrNo.EINVAL);
            return 0;
        }

        if (size == 0 || count == 0)
        {
            return 0;
        }

        nuint result = 0;
        if (streamHandle.Stream is not null)
        {
            var tempbuffer = new byte[size];
            for (; result < count; result++)
            {
                var readBytes = streamHandle.Stream!.Read(tempbuffer, 0, (int)size);
                for (nuint i = 0; i < (nuint)readBytes; i++)
                {
                    *(byte*)buffer = tempbuffer[i];
                    buffer = ((byte*)buffer) + 1;
                }

                if ((int)size != readBytes)
                {
                    break;
                }
            }
        }
        else
        {
            using var reader = streamHandle.Reader!();
            try
            {
                for (; result < count; result++)
                {
                    for (nuint i = 0; i < size; i++)
                    {
                        var value = reader.Read();
                        if (value == -1)
                        {
                            return result;
                        }

                        *(byte*)buffer = (byte)value;
                        buffer = (byte*)buffer + 1;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                StdLibFunctions.SetErrNo(ErrNo.EIO);
                return result;
            }
        }

        return result;
    }

    public static nuint FWrite(void* buffer, nuint size, nuint count, void* stream)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle is null || streamHandle.Writer is null)
        {
            StdLibFunctions.SetErrNo(ErrNo.EINVAL);
            return 0;
        }

        if (size == 0 || count == 0)
        {
            return 0;
        }

        nuint result = 0;
        if (streamHandle.Stream is not null)
        {
            var tempbuffer = new byte[size];
            for (; result < count; result++)
            {
                for (nuint i = 0; i < size; i++)
                {
                    tempbuffer[i] = *(byte*)buffer;
                    buffer = (byte*)buffer + 1;
                }

                streamHandle.Stream.Write(tempbuffer, 0, (int)size);
            }

            FFlush(streamHandle);
        }
        else
        {
            using var writer = streamHandle.Writer!();
            for (; result < count; result++)
            {
                for (nuint i = 0; i < size; i++)
                {
                    var value = *(byte*)buffer;
                    buffer = (byte*)buffer + 1;
                    writer.Write((char)value);
                }
            }

            writer.Flush();
        }

        return result;
    }

    public static int FFlush(void* stream)
    {
        var streamHandle = GetStream(stream);
        if (streamHandle == null)
        {
            Console.WriteLine("EBADF");
            return ErrNo.EBADF;
        }

        FFlush(streamHandle);
        return 0;
    }

    internal static StreamHandle? GetStream(void* filePtr)
    {
        var handle = (IntPtr)filePtr;

        var handleValue = handle.ToInt64();
        if (handleValue is _stdIn or _stdOut or _stdErr)
        {
            return Handles[(int)handleValue];
        }

        var gchAddr = Marshal.ReadIntPtr(handle);
        var gch = GCHandle.FromIntPtr(gchAddr);

        return gch.Target as StreamHandle;
    }

    internal static void* AddStream(StreamHandle stream)
    {
        var gch = GCHandle.Alloc(stream);
        var handle = GCHandle.ToIntPtr(gch);

        var ptr = Marshal.AllocHGlobal(sizeof(IntPtr));
        Marshal.WriteIntPtr(ptr, handle);
        return (void*)ptr;
    }

    internal static bool FreeStream(void* filePtr)
    {
        var handle = (IntPtr)filePtr;

        var gchAddr = Marshal.ReadIntPtr(handle);
        var gch = GCHandle.FromIntPtr(gchAddr);

        if (gch.Target is not StreamHandle) return false;

        gch.Free();
        Marshal.FreeHGlobal(handle);
        return true;
    }

    private static void FFlush(StreamHandle streamHandle)
    {
        if (streamHandle.Stream is FileStream fileStream)
        {
            fileStream.Flush(true);
        }
        else
        {
            streamHandle.Stream?.Flush();
        }
    }

    class BytePtrTextWriter(byte* ptr) : TextWriter
    {
        public override Encoding Encoding => throw new NotImplementedException();

        public override void Write(char value)
        {
            *ptr = (byte)value;
            ptr++;
        }

        public override void Write(string? value)
        {
            if (value is null) return;

            foreach (var c in value)
            {
                Write(c);
            }
        }
    }

    class AutoGrowStream : MemoryStream
    {
        private readonly byte** _ptr;
        private readonly nuint* _sizeloc;

        public AutoGrowStream(byte** ptr, nuint* sizeloc) : base()
        {
            _ptr = ptr;
            *_ptr = null;
            _sizeloc = sizeloc;
        }

        public override void Flush()
        {
            base.Flush();
            if (*_ptr != null)
            {
                StdLibFunctions.Free(*_ptr);
            }

            *_ptr = (byte*)StdLibFunctions.Malloc((UIntPtr)Length + 1);
            *_sizeloc = (nuint)Length;
            Marshal.Copy(GetBuffer(), 0, (IntPtr)(*_ptr), (int)Length);
            (*_ptr)[Length] = 0;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (loc == SeekOrigin.Begin && offset > Length)
            {
                Write(new byte[offset - Length], 0, (int)(offset - Length));
            }

            return base.Seek(offset, loc);
        }

        public override void Close()
        {
            base.Close();
        }
    }
}

internal static class FormattingExtensions
{
    public static bool IsIn(this char c, char start, char end) =>
        c >= start && c <= end;

    public static bool IsAscii(this char c) =>
        c.IsIn((char)0, (char)255);

    public static bool IsAsciiDigit(this char c) =>
        c.IsIn('0', '9');

    public static bool IsOctalDigit(this char c) =>
        c.IsAsciiDigit() && c <= '7';

    public static bool IsHexDigit(this char c) =>
        c.IsAsciiDigit() || c.IsIn('A', 'F') || c.IsIn('a', 'f');

    public static bool IsSizeAttribute(this char c) =>
        c is 'h' or 'l' or 'j';

    public static int Num(this char c)
    {
        if (c.IsAsciiDigit())
            return c - '0';
        if (c.IsHexDigit())
            return c - 'A' + 10;

        return c;
    }
}

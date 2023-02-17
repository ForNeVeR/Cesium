namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    public static void PutS(CPtr<byte> str)
    {
        try
        {
            Console.Write(RuntimeHelpers.Unmarshal(str.AsPtr()));
            // return 0; // TODO[#156]: Uncomment
        }
        catch (Exception) // TODO[#154]: Exception handling.
        {
            // const int EOF = -1; // TODO[#155]: Extract to some common place.
            // return EOF; // TODO[#156]: Uncomment
        }
    }

    public static int PrintF(CPtr<byte> str, VoidPtr varargs)
    {
        var formatString = RuntimeHelpers.Unmarshal(str.AsPtr());
        if (formatString == null)
        {
            return -1;
        }

        int currentPosition = 0;
        var formatStartPosition = formatString.IndexOf('%', currentPosition);
        int consumedArgs = 0;
        int consumedBytes = 0;
        while (formatStartPosition >= 0)
        {
            var lengthTillPercent = formatStartPosition - currentPosition;
            Console.Write(formatString.Substring(currentPosition, lengthTillPercent));
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
                    string? stringValue = RuntimeHelpers.Unmarshal((byte*)varargs.AsPtr<long>()[consumedArgs]);
                    Console.Write(stringValue);
                    consumedBytes += stringValue?.Length ?? 0;
                    consumedArgs++;
                    break;
                case "c":
                    Console.Write((char)(byte)varargs.AsPtr<long>()[consumedArgs]);
                    consumedBytes++;
                    consumedArgs++;
                    break;
                case "d":
                case "li":
                case "i":
                    int intValue = (int)varargs.AsPtr<long>()[consumedArgs];
                    var intValueString = intValue.ToString();
                    Console.Write(intValueString);
                    consumedBytes += intValueString.Length;
                    consumedArgs++;
                    break;
                case "u":
                case "lu":
                    uint uintValue = (uint)varargs.AsPtr<long>()[consumedArgs];
                    var uintValueString = uintValue.ToString();
                    Console.Write(uintValueString);
                    consumedBytes += uintValueString.Length;
                    consumedArgs++;
                    break;
                case "f":
                    var floatNumber = varargs.AsPtr<double>()[consumedArgs];
                    string floatNumberString = floatNumber.ToString("F6");
                    Console.Write(floatNumberString);
                    consumedBytes += floatNumberString.Length;
                    consumedArgs++;
                    break;
                case "p":
                    nint pointerValue = varargs.AsPtr<nint>()[consumedArgs];
                    string pointerValueString = pointerValue.ToString("X");
                    Console.Write(pointerValueString);
                    consumedBytes += pointerValueString.Length;
                    consumedArgs++;
                    break;
                default:
                    throw new FormatException($"Format specifier {formatSpecifier} is not supported");
            }

            currentPosition = formatStartPosition + addition + 1;
            formatStartPosition = formatString.IndexOf('%', currentPosition);
        }

        string remainderString = formatString.Substring(currentPosition);
        Console.Write(remainderString);
        return consumedBytes + remainderString.Length;
    }
}

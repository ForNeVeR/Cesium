namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the ctype.h
/// </summary>
public unsafe static class CTypeFunctions
{
    public static int ToUpper(int value)
    {
        return IsLower(value) != 0 ? char.ToUpper((char)value) : value;
    }

    public static int ToLower(int value)
    {
        return IsUpper(value) != 0 ? char.ToLower((char)value) : value;
    }

    public static int IsAlnum(int value)
    {
        return char.IsLetterOrDigit((char)value) ? 1 : 0;
    }

    public static int IsAlpha(int value)
    {
        return char.IsLetter((char)value) ? 1 : 0;
    }

    public static int IsLower(int value)
    {
        return (value >= 'a' && value <= 'z') ? 1 : 0;
    }

    public static int IsUpper(int value)
    {
        return (value >= 'A' && value <= 'Z') ? 1 : 0;
    }

    public static int IsDigit(int value)
    {
        return (value >= '0' && value <= '9') ? 1 : 0;
    }

    public static int IsXDigit(int value)
    {
        return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f') ? 1 : 0;
    }

    public static int IsCntrl(int value)
    {
        return (value <= 31) || (value == 127) ? 1 : 0;
    }

    public static int IsGraph(int value)
    {
        return (value >= 33 && value <= 126) ? 1 : 0;
    }

    public static int IsPunct(int value)
    {
        return (value >= 33 && value <= 47) || (value >= 58 && value <= 64) || (value >= 91 && value <= 96) || (value >= 123 && value <= 126) ? 1 : 0;
    }

    public static int IsSpace(int value)
    {
        return value switch
        {
            0x20 => 1,
            0x0c => 1,
            0x0a => 1,
            0x0d => 1,
            0x09 => 1,
            0x0b => 1,
            _ => 0,
        };
    }

    public static int IsBlank(int value)
    {
        return (value == 9) || (value == 32) ? 1 : 0;
    }

    public static int IsPrint(int value)
    {
        return (value >= 32 && value <= 126) ? 1 : 0;
    }
}

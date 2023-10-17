namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the ctype.h
/// </summary>
public unsafe static class CTypeFunctions
{
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
}

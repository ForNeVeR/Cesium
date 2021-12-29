namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the string.h
/// </summary>
public unsafe static class StringFunctions
{
    public static uint StrLen(char* str)
    {
        return (uint)new string(str).Length;
    }
}

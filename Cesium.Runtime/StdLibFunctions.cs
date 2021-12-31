namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdlib.h
/// </summary>
public unsafe static class StdlibFunctions
{
    public static int Abs(int value)
    {
        return Math.Abs(value);
    }
}

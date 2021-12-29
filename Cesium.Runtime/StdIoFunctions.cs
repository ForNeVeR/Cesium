namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    public static void PutS(char* format)
    {
        Console.Write(new string(format));
    }
}

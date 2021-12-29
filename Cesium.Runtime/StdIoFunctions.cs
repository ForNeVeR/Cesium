namespace Cesium.Runtime;

using System.Runtime.InteropServices;

/// <summary>
/// Functions declared in the stdio.h
/// </summary>
public unsafe static class StdIoFunctions
{
    public static void PutS(byte* format)
    {
        Console.Write(Marshal.PtrToStringUTF8((nint)format));
    }
}

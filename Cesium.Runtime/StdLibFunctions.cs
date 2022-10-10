namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdlib.h
/// </summary>
public unsafe static class StdLibFunctions
{
    public const int RAND_MAX = 0x7fff;
    private static System.Random shared = new();

    public static int Abs(int value)
    {
        if (value == int.MinValue) return int.MinValue;
        return Math.Abs(value);
    }

    public static void Exit(int exitCode)
    {
        RuntimeHelpers.Exit(exitCode);
    }

    public static int Rand()
    {
        return shared.Next(RAND_MAX);
    }

    public static void SRand(uint seed)
    {
        shared = new Random(unchecked((int)seed));
    }
}

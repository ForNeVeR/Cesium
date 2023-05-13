using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdlib.h
/// </summary>
public unsafe static class StdLibFunctions
{
    public const int RAND_MAX = 0x7FFFFFFF;
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
        shared = new Random((int)seed);
    }

    public static int System(byte* command)
    {
        string? shellCommand = StdIoFunctions.Unmarshal(command);
        var startParameters = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo("cmd", $"/c {shellCommand}")
            : new ProcessStartInfo("/bin/sh", $"-c \"{shellCommand}\"");
        Process? pa11yCiInstallation = Process.Start(startParameters);
        if (pa11yCiInstallation is null)
        {
            return 8 /*ENOEXEC*/;
        }

        pa11yCiInstallation.WaitForExit();
        return pa11yCiInstallation.ExitCode;
    }

    public static void* Malloc(UIntPtr size)
    {
#if NETSTANDARD
        return (void*)Marshal.AllocHGlobal((int)size);
#else
        return NativeMemory.Alloc(size);
#endif
    }

    public static void Free(void* ptr)
    {
#if NETSTANDARD
        Marshal.FreeHGlobal((IntPtr)ptr);
#else
        NativeMemory.Free(ptr);
#endif
    }

    public static void* Realloc(void* ptr, UIntPtr size)
    {
#if NETSTANDARD
        Free(ptr);
        return Malloc(size);
#else
        return NativeMemory.Realloc(ptr, size);
#endif
    }

    public static void* Сalloc(UIntPtr num, UIntPtr size)
    {
#if NETSTANDARD
        var ptr = Marshal.AllocHGlobal((int)size * (int)num);
        for (var i =0;i < (int)size * (int)num;i++)
        {
            *((byte*)ptr + i) = 0;
        }
        return (void*)ptr;
#else
        return NativeMemory.AllocZeroed(num, size);
#endif
    }

    public static void* AlignedAlloc(UIntPtr alignment, UIntPtr size)
    {
#if NETSTANDARD
        throw new NotImplementedException();
#else
        return NativeMemory.AlignedAlloc(size, alignment);
#endif
    }
}

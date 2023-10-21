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

    public static void Abort()
    {
        Environment.FailFast("Aborted");
    }

    public static int System(byte* command)
    {
        string? shellCommand = StdIoFunctions.Unmarshal(command);
        if (shellCommand is null)
        {
            return 8 /*ENOEXEC*/;
        }

        var startParameters = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo($"cmd", $"/c {shellCommand}") { UseShellExecute = false }
            : new ProcessStartInfo("/bin/sh", $"-c \"{EscapeShell(shellCommand)}\"");
        Process? process = Process.Start(startParameters);
        if (process is null)
        {
            return 8 /*ENOEXEC*/;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static string EscapeShell(string command)
    {
        return command.Replace("\"", "\\\"");
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

    public static int Atoi(byte* ptr)
    {
        var str = StdIoFunctions.Unmarshal(ptr);
        return Convert.ToInt32(str);
    }
}

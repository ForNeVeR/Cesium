using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the stdlib.h
/// </summary>
public unsafe static class StdLibFunctions
{
    public const int RAND_MAX = 0x7FFFFFFF;
    private static Random shared = new();

    private class EnvVarsStorage
    {
        public byte* Values { get; }
        public Dictionary<string, int> Indices { get; }

        public EnvVarsStorage(int bufferLength, Dictionary<string, int> indices)
        {
            Values = (byte*)Malloc((nuint)bufferLength);
            Indices = indices;
        }
    }

    [FixedAddressValueType]
    private static int errNo;

    private static readonly Lazy<EnvVarsStorage> _envVarsStorage = new(InitEnvVarsStorage);

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
        string? shellCommand = RuntimeHelpers.Unmarshal(command);
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

    public static int* GetErrNo()
    {
        fixed (int* errNoPtr = &errNo)
        {
            return errNoPtr;
        }
    }

    internal static void SetErrNo(int newErrorCode)
    {
        errNo = newErrorCode;
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

    public static byte* GetEnv(byte* ptr)
    {
        var envKey = StdIoFunctions.Unmarshal(ptr);

        if (envKey is null)
        {
            return null;
        }

        var storage = _envVarsStorage.Value;

        if (storage.Indices.TryGetValue(envKey, out var envValueIndex))
        {
            return storage.Values + envValueIndex;
        }

        return null;
    }

    public static long StrToL(byte* str, byte** str_end, int @base)
    {
        byte* current = str;
        byte currentChar;
        do
        {
            currentChar = *current++;
        }
        while (CTypeFunctions.IsSpace(currentChar) != 0);

        bool negate = false;
        if (currentChar == '-')
        {
            negate = true;
            currentChar = *current++;
        }
        else if (currentChar == '+')
            currentChar = *current++;

        if ((@base == 0 || @base == 16) &&
            currentChar == '0' && (*current == 'x' || *current == 'X'))
        {
            currentChar = current[1];
            current += 2;
            @base = 16;
        }

        if (@base == 0)
        {
            @base = currentChar == '0' ? 8 : 10;
        }

        long cutoff = negate ? long.MinValue : long.MaxValue;
        long cutlim = negate ? -(cutoff % @base) : (cutoff % @base);
        cutoff /= @base;
        if (negate) cutoff = -cutoff;
        long result = 0;
        int foundSomething = 0;
        for (result = 0, foundSomething = 0; ; currentChar = *current++)
        {
            if (CTypeFunctions.IsDigit(currentChar) != 0)
                currentChar -= (byte)'0';
            else if (CTypeFunctions.IsAlpha(currentChar) != 0)
                currentChar -= (byte)((CTypeFunctions.IsUpper(currentChar) != 0) ? (byte)'A' - 10 : (byte)'a' - 10);
            else
                break;
            if (currentChar >= @base)
                break;
            if (foundSomething < 0 || result > cutoff || (result == cutoff && currentChar > cutlim))
                foundSomething = -1;
            else
            {
                foundSomething = 1;
                result *= @base;
                result += currentChar;
            }
        }

        if (foundSomething < 0)
        {
            result = negate ? long.MinValue : long.MaxValue;
            errNo = 34 /*ERANGE*/;
        }
        else if (negate)
            result = -result;
        if (str_end != null)
            *str_end = (foundSomething != 0) ? current - 1 : str;
        return result;
    }

    public static ulong StrToUL(byte* str, byte** str_end, int @base)
    {
        return (ulong)StrToL(str, str_end, @base);
    }

    private static EnvVarsStorage InitEnvVarsStorage()
    {
        var processEnvs = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        var indices = new Dictionary<string, int>(
            capacity: processEnvs.Count,
            // Windows variables are case-insensitive
            comparer: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : null);

        var enc = Encoding.UTF8;
        var totalBufferLength = 0;

        foreach (DictionaryEntry entry in processEnvs)
        {
            var keyStr = (string)entry.Key;

            if (entry.Value is string entryValueStr)
            {
                indices.Add(keyStr, totalBufferLength);
                totalBufferLength += enc.GetByteCount(entryValueStr) + 1; // each value is null-terminated
            }
        }

        // Last value has additional null-terminator
        var storage = new EnvVarsStorage(totalBufferLength + 1, indices);

        foreach (KeyValuePair<string, int> kvp in indices)
        {
            var envVarKey = kvp.Key;
            var envVarIndex = kvp.Value;

            if (processEnvs[envVarKey] is string envValueStr)
            {
                var envValueBytes = enc.GetBytes(envValueStr);

#if NETSTANDARD
                for (int i = 0; i < envValueBytes.Length; i++)
                {
                    storage.Values[envVarIndex + i] = envValueBytes[i];
                }
#else
                envValueBytes.AsSpan().CopyTo(new Span<byte>(storage.Values + envVarIndex, envValueBytes.Length));
#endif

                storage.Values[envVarIndex + envValueBytes.Length] = 0;
            }
        }

        storage.Values[totalBufferLength] = 0;
        return storage;
    }
}

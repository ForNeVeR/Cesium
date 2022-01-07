using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Cesium.Runtime;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static unsafe class RuntimeHelpers
{
    public static byte*[] ArgsToArgv(string[] strings)
    {
        var encoding = Encoding.UTF8;
        byte* AllocateUtf8String(string s)
        {
            var bytes = encoding.GetBytes(s);
            var buffer = (byte*)Marshal.AllocHGlobal(s.Length + 1);
            Marshal.Copy(bytes, 0, (IntPtr)buffer, bytes.Length);
            buffer[s.Length] = 0;
            return buffer;
        }

        // Last item should be a null pointer; the first one we'll allocate from the executable path, so + 2:
        var pointers = new byte*[strings.Length + 2];

        var executablePath = Assembly.GetEntryAssembly()?.Location ?? "";
        pointers[0] = AllocateUtf8String(executablePath);
        for (var i = 0; i < strings.Length; ++i)
            pointers[i + 1] = AllocateUtf8String(strings[i]);

        return pointers;
    }

    public static void FreeArgv(byte*[] argv)
    {
        for (var i = 0; i < argv.Length - 1; ++i)
        {
            Marshal.FreeHGlobal((IntPtr)argv[i]);
        }
    }
}

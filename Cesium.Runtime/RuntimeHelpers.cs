using System.Runtime.InteropServices;
using System.Text;

namespace Cesium.Runtime;

public static unsafe class RuntimeHelpers
{
    public static byte*[] ArgsToArgv(string[] strings)
    {
        var encoding = Encoding.UTF8;

        var pointers = new byte*[strings.Length + 1]; // last item should be a null pointer
        for (var i = 0; i < strings.Length; ++i)
        {
            var s = strings[i];
            var bytes = encoding.GetBytes(s);
            var buffer = Marshal.AllocHGlobal(s.Length + 1);
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            var ptr = (byte*)buffer;
            ptr![bytes.Length] = 0;
            pointers[i] = ptr;
        }

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

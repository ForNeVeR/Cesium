using System;
using System.Runtime.InteropServices;

namespace Cesium.Runtime;
public static class CesiumFunctions
{
    public static int GetOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 1;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return 2;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return 3;
        return 0;
    }
}

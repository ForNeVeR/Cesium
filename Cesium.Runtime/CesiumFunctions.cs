// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Text;

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

    internal static unsafe string? Unmarshal(byte* str)
    {
#if NETSTANDARD
        Encoding encoding = Encoding.UTF8;
        int byteLength = 0;
        byte* search = str;
        while (*search != '\0')
        {
            byteLength++;
            search++;
        }

        int stringLength = encoding.GetCharCount(str, byteLength);
        string s = new('\0', stringLength);
        fixed (char* pTempChars = s)
        {
            encoding.GetChars(str, byteLength, pTempChars, stringLength);
        }

        return s;
#else
        return Marshal.PtrToStringUTF8((nint)str);
#endif
    }

    internal static unsafe UTF8String MarshalStr(string? str)
    {
        Encoding encoding = Encoding.UTF8;
        if (str is null)
        {
            return UTF8String.NullString;
        }

        var bytes = encoding.GetBytes(str);
        var storage = (byte*)StdLibFunctions.Malloc((nuint)bytes.Length + 1);
        for (var i = 0; i < bytes.Length; i++)
        {
            storage[i] = bytes[i];
        }

        storage[bytes.Length] = 0;
        return storage;
    }
}

// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Compiler;

internal static class RuntimeConfig
{
    public static string EmitNet6() => """
    {
      "runtimeOptions": {
        "tfm": "net6.0",
        "rollForward": "Major",
        "framework": {
          "name": "Microsoft.NETCore.App",
          "version": "6.0.0"
        }
      }
    }
    """.ReplaceLineEndings("\n");

    public static string EmitNet10() => """
    {
      "runtimeOptions": {
        "tfm": "net10.0",
        "rollForward": "Major",
        "framework": {
          "name": "Microsoft.NETCore.App",
          "version": "10.0.0"
        }
      }
    }
    """.ReplaceLineEndings("\n");
}

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

    public static string EmitNet8() => """
    {
      "runtimeOptions": {
        "tfm": "net8.0",
        "rollForward": "Major",
        "framework": {
          "name": "Microsoft.NETCore.App",
          "version": "8.0.0"
        }
      }
    }
    """.ReplaceLineEndings("\n");
}

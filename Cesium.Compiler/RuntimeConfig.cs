namespace Cesium.Compiler;

internal static class RuntimeConfig
{
    public static string EmitNet7() => """
    {
      "runtimeOptions": {
        "tfm": "net7.0",
        "rollForward": "Major",
        "framework": {
          "name": "Microsoft.NETCore.App",
          "version": "7.0.0"
        }
      }
    }
    """.ReplaceLineEndings("\n");
}

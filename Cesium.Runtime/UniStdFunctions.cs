namespace Cesium.Runtime;

internal static class UniStdFunctions
{
    public static int Sleep(double duration)
    {
        Thread.Sleep((int)(duration * 1000));
        return 0;
    }
}

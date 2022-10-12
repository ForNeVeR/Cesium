namespace Cesium.Runtime;

public static class UniStdFunctions
{
    public static int Sleep(uint duration)
    {
        Thread.Sleep(duration * 1000);
        return 0;
    }
}

namespace Cesium.Runtime;

public static class UniStdFunctions
{
    public static int Sleep(uint duration)
    {
        Thread.Sleep((int)(duration * 1000)F);
        return 0;
    }
}

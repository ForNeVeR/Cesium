namespace Cesium.Runtime;

public static unsafe class TimeFunctions
{
    public static long Time(long* time)
    {
        var result = (DateTime.UtcNow - new DateTime(0)).TotalSeconds;
        if (time is not null)
        {
            *time = (long)result;
        }

        return (long)result;
    }
}

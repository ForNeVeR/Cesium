namespace Cesium.Runtime;

public static class ConioFunctions
{
    public static int KbHit()
    {
        return Console.KeyAvailable ? 1 : 0;
    }

    public static int GetCh()
    {
        return (int)Console.ReadKey(true).KeyChar;
    }
}

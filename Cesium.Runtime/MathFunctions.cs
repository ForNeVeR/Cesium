namespace Cesium.Runtime;

public unsafe static class MathFunctions
{
    public static float SqrtF(float value)
    {
#if NETSTANDARD2_0
        throw new NotImplementedException();
#else
        return MathF.Sqrt(value);
#endif
    }
    public static double Sqrt(double value)
    {
        return Math.Sqrt(value);
    }
    public static float LogF(float value)
    {
#if NETSTANDARD2_0
        throw new NotImplementedException();
#else
        return MathF.Log(value);
#endif
    }
    public static double Log(double value)
    {
        return Math.Log(value);
    }
    public static float CosF(float value)
    {
#if NETSTANDARD2_0
        throw new NotImplementedException();
#else
        return MathF.Cos(value);
#endif
    }
    public static double Cos(double value)
    {
        return Math.Cos(value);
    }
    public static float SinF(float value)
    {
#if NETSTANDARD2_0
        throw new NotImplementedException();
#else
        return MathF.Sin(value);
#endif
    }
    public static double Sin(double value)
    {
        return Math.Sin(value);
    }
}

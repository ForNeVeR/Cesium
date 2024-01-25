namespace Cesium.Preprocessor.Tests;

internal static class ExpressionTestExtensions
{
    public static bool ToBoolean(this string text)
    {
        return text switch
        {
            "1" => true,
            "0" => false,
            _ => throw new NotSupportedException()
        };
    }
}

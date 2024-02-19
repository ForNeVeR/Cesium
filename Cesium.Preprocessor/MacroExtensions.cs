using System.Globalization;
using Cesium.Core;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal static class MacroExtensions
{
    public static bool AsBoolean(this string? macroValue, Location location)
    {
        if (macroValue == "0")
            return false;

        if (macroValue is null)
            throw new PreprocessorException(location, "No value provided where an integer was expected.");

        if (int.TryParse(macroValue, CultureInfo.InvariantCulture, out _))
            return true;

        throw new PreprocessorException(location, $"Invalid integer constant expression: {macroValue}.");
    }

    public static bool AsBoolean(this int num) => num != 0;
}

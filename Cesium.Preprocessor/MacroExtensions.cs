using System.Globalization;
using Cesium.Core;

namespace Cesium.Preprocessor;

internal static class MacroExtensions
{
    public static bool AsBoolean(this string? macroValue)
    {
        if (macroValue == "0")
            return false;

        if (macroValue is null)
            throw new PreprocessorException("Invalid integer constant expression");

        if (int.TryParse(macroValue, CultureInfo.InvariantCulture, out _))
            return true;

        throw new PreprocessorException("Invalid integer constant expression");
    }

    public static bool AsBoolean(this int num) => num != 0;
}

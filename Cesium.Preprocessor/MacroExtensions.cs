using Cesium.Core;
using System.Globalization;

namespace Cesium.Preprocessor;

internal static class MacroExtensions
{
    public static bool AsBoolean(this string? macroValue)
    {
        if (macroValue == "0")
            return false;

        if (int.TryParse(macroValue, CultureInfo.InvariantCulture, out _))
            return true;

        throw new PreprocessorException("Invalid integer constant expression");
    }
}

using System.Globalization;
using Yoakke.SynKit.Text;

namespace Cesium.Core;

/// <param name="Line">One-based, for presentation purposes. May be unknown.</param>
/// <param name="Column">One-based, for presentation purposes. May be unknown.</param>
public record ErrorLocationInfo(string FilePath, int? Line, int? Column) : IComparable
{
    public override string ToString()
    {
        static string IntToString(int? i) => i?.ToString(CultureInfo.InvariantCulture) ?? "<unknown>";
        return $"{FilePath}:{IntToString(Line)}:{IntToString(Column)}";
    }

    public static implicit operator ErrorLocationInfo(Location location)
    {
        var startLocation = location.Range.Start;
        return new ErrorLocationInfo(
            location.File?.Path ?? "<unknown file>",
            Line: startLocation.Line + 1,
            Column: startLocation.Column + 1);
    }

    public int CompareTo(object? obj)
    {
        if (obj is not ErrorLocationInfo other)
            throw new ArgumentException("Cannot compare to non-ErrorLocationInfo.", nameof(obj));

        if (FilePath != other.FilePath)
            throw new ArgumentException("Cannot compare to an ErrorLocationInfo from another file.", nameof(obj));

        return (Line, Column).CompareTo((other.Line, other.Column));
    }
}

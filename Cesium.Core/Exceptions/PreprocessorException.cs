namespace Cesium.Core;

public sealed class PreprocessorException(ErrorLocationInfo location, string message)
    : CesiumException($"{location}: {message}")
{
    public ErrorLocationInfo? Location { get; } = location;
    public string RawMessage { get; } = message;
}

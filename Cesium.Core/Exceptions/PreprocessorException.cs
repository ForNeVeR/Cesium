namespace Cesium.Core;

public sealed class PreprocessorException(SourceLocationInfo location, string message)
    : CesiumException($"{location}: {message}")
{
    public SourceLocationInfo? Location { get; } = location;
    public string RawMessage { get; } = message;
}

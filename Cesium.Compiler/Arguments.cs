using CommandLine;

namespace Cesium.Compiler;

public class Arguments
{
    [Value(index: 0)]
    public string InputFilePath { get; init; }

    [Value(index: 1)]
    public string OutputFilePath { get; init; }
}

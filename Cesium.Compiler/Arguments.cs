using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace Cesium.Compiler;

public enum TargetFrameworkKind
{
    Net,
    NetFramework,
    NetStandard
}

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Arguments
{
    [Value(0)] public IList<string> InputFilePaths { get; init; } = null!;

    [Option('o', "out", Required = true)] public string OutputFilePath { get; init; } = null!;

    [Option("framework", Default = TargetFrameworkKind.Net)]
    public TargetFrameworkKind Framework { get; init; }

    [Option("nologo", HelpText = "Suppress compiler banner message")]
    public bool NoLogo { get; set; }
}

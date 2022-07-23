using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Mono.Cecil;

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
    [Value(0)]
    public IList<string> InputFilePaths { get; init; } = null!;

    [Option('o', "out", Required = true)]
    public string OutputFilePath { get; init; } = null!;

    [Option("framework", Default = TargetFrameworkKind.Net)]
    public TargetFrameworkKind Framework { get; init; }

    [Option("modulekind")]
    public ModuleKind? ModuleKind { get; init; } = null!;

    [Option("nologo", HelpText = "Suppress compiler banner message")]
    public bool NoLogo { get; set; }

    [Option("namespace", HelpText = "Sets default namespace instead of \"global\"")]
    public string Namespace { get; init; } = "";

    [Option("globalclass", HelpText = "Sets default global class instead of \"<Module>\"")]
    public string GlobalClass { get; init; } = "";

}

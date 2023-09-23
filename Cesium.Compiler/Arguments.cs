using System.Diagnostics.CodeAnalysis;
using Cesium.CodeGen;
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

    [Option('o', "out")]
    public string OutputFilePath { get; init; } = null!;

    [Option("framework", Default = TargetFrameworkKind.Net)]
    public TargetFrameworkKind Framework { get; init; }

    [Option("arch", Default = TargetArchitectureSet.Dynamic)]
    public TargetArchitectureSet TargetArchitectureSet { get; init; }

    [Option("modulekind")]
    public ModuleKind? ModuleKind { get; init; } = null!;

    [Option("nologo", HelpText = "Suppress compiler banner message")]
    public bool NoLogo { get; set; }

    [Option("namespace", HelpText = "Sets default namespace instead of \"global\"")]
    public string Namespace { get; init; } = "";

    [Option("globalclass", HelpText = "Sets default global class instead of \"<Module>\"")]
    public string GlobalClass { get; init; } = "";

    [Option("import", HelpText = "Provides path to assemblies which would be added as references automatically into resulting executable.")]
    public IList<string> DefaultImportAssemblies { get; init; } = null!;

    [Option("corelib", HelpText = "Sets path to CoreLib assembly")]
    public string? CoreLib { get; init; }

    [Option("runtime", HelpText = "Sets path to Cesium C Runtime assembly")]
    public string? CesiumCRuntime { get; init; }

    [Option('O', HelpText = "Set the optimization level")]
    public int OptimizationLevel { get; init; } = 0;

    [Option('W', HelpText = "Enable warnings set")]
    public string WarningsSet { get; init; } = "";

    [Option('E', HelpText = "Produce preprocessed file")]
    public bool ProducePreprocessedFile { get; init; } = false;

    [Option('I', HelpText = "Adds include directory of header files")]
    public IEnumerable<string> IncludeDirectories { get; init; } = Array.Empty<string>();

    [Option('D', HelpText = "Define constants for preprocessor")]
    public IEnumerable<string> DefineConstant { get; init; } = Array.Empty<string>();

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cesium.Sdk;

/*
  -o, --out        Sets path for the output assembly file
  --framework      (Default: Net)  Valid values: Net, NetFramework, NetStandard
  --arch           (Default: Dynamic)  Valid values: Dynamic, Bit32, Bit64
  --modulekind     Valid values: Dll, Console, Windows, NetModule
  --nologo         Suppress compiler banner message
  --namespace      Sets default namespace instead of "global"
  --globalclass    Sets default global class instead of "<Module>"
  --import         Provides path to assemblies which would be added as references automatically into resulting executable.
  --corelib        Sets path to CoreLib assembly
  --runtime        Sets path to Cesium C Runtime assembly
  -O               Set the optimization level
  -W               Enable warnings set
  -D               Define constants for preprocessor
  --help           Display this help screen.
  --version        Display version information.
  value pos. 0
 */

public class CesiumCompile : Task
{
    [Required] public ITaskItem CompilerExe { get; set; } = null!;
    [Required] public ITaskItem[] InputFiles { get; set; } = null!;
    [Required] public ITaskItem OutputFile { get; set; } = null!;

    public string? Namespace { get; set; }
    public string? Framework { get; set; }
    public string? Architecture { get; set; }
    public string? ModuleType { get; set; }
    public ITaskItem? CoreLibPath { get; set; }
    public ITaskItem? RuntimePath { get; set; }
    public ITaskItem[] ImportItems { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] PreprocessorItems { get; set; } = Array.Empty<ITaskItem>();
    public bool DryRun = false;

    [Output] public string? ResultingCommandLine { get; private set; }

    public override bool Execute()
    {
        if (!TryValidate(out var options) || options is null)
        {
            return false;
        }

        var argumentsBuilder = new CommandArgumentsBuilder();
        foreach (var argument in CollectCommandLineArguments(options))
        {
            argumentsBuilder.Argument(argument);
        }

        var compilerProcess = new Process
        {
            StartInfo =
            {
                FileName = options.CompilerExe,
                Arguments = argumentsBuilder.Build(),
                UseShellExecute = false,
            }
        };

        ResultingCommandLine = $"{compilerProcess.StartInfo.FileName} {compilerProcess.StartInfo.Arguments}";
        if (!DryRun)
        {
            compilerProcess.Start();
            compilerProcess.WaitForExit();
        }

        return true;
    }

    private static (bool, FrameworkKind?) TryParseFramework(string? framework) => framework switch
    {
        null => (true, null),
        nameof(FrameworkKind.Net) => (true, FrameworkKind.Net),
        nameof(FrameworkKind.NetFramework) => (true, FrameworkKind.NetFramework),
        nameof(FrameworkKind.NetStandard) => (true, FrameworkKind.NetStandard),
        _ => (false, null)
    };

    private static (bool, ArchitectureKind?) TryParseArchitectureKind(string? archKind) => archKind switch
    {
        null => (true, null),
        nameof(ArchitectureKind.Dynamic) => (true, ArchitectureKind.Dynamic),
        nameof(ArchitectureKind.Bit32) => (true, ArchitectureKind.Bit32),
        nameof(ArchitectureKind.Bit64) => (true, ArchitectureKind.Bit64),
        _ => (false, null)
    };

    private static (bool, ModuleKind?) TryParseModuleKind(string? moduleKind) => moduleKind switch
    {
        null => (true, null),
        nameof(ModuleKind.Dll) => (true, ModuleKind.Dll),
        nameof(ModuleKind.Console) => (true, ModuleKind.Console),
        nameof(ModuleKind.Windows) => (true, ModuleKind.Windows),
        nameof(ModuleKind.NetModule) => (true, ModuleKind.NetModule),
        _ => (false, null)
    };

    private bool TryValidate(out ValidatedOptions? options)
    {
        options = null;
        var success = true;


        if (!string.IsNullOrWhiteSpace(CompilerExe.ItemSpec) && !File.Exists(CompilerExe.ItemSpec))
        {
            ReportValidationError("CES1000", $"Compiler executable doesn't exist under path '{CompilerExe?.ItemSpec}'");
            success = false;
        }

        var (isFrameworkValid, framework) = TryParseFramework(Framework);
        if (!isFrameworkValid)
        {
            var validValues = Enum.GetValues<FrameworkKind>().Select(kind => kind.ToString());
            ReportValidationError("CES1004", $"Framework should be in range: '{string.Join(", ", validValues)}'");
            success = false;
        }

        var (isArchValid, arch) = TryParseArchitectureKind(Architecture);
        if (!isArchValid)
        {
            var validValues = Enum.GetValues<ArchitectureKind>().Select(kind => kind.ToString());
            ReportValidationError("CES1005", $"Architecture should be in range: '{string.Join(", ", validValues)}'");
            success = false;
        }

        var (isModuleKindValid, moduleKind) = TryParseModuleKind(ModuleType);
        if (!isModuleKindValid)
        {
            var validValues = Enum.GetValues<ModuleKind>().Select(kind => kind.ToString());
            ReportValidationError("CES1006", $"ModuleKind should be in range: '{string.Join(", ", validValues)}'");
            success = false;
        }

        var missingCompileItems = InputFiles.Where(item => !File.Exists(item.ItemSpec)).ToList();
        foreach (var item in missingCompileItems)
        {
            ReportValidationError("CES1001", $"Source file doesn't exist: '{item.ItemSpec}'");
            success = false;
        }

        if (!string.IsNullOrWhiteSpace(CoreLibPath?.ItemSpec) && !File.Exists(CoreLibPath.ItemSpec))
        {
            ReportValidationError("CES1002", $"CorLib doesn't exist under path '{CoreLibPath?.ItemSpec}'");
            success = false;
        }

        if (!string.IsNullOrWhiteSpace(RuntimePath?.ItemSpec) && !File.Exists(RuntimePath.ItemSpec))
        {
            ReportValidationError("CES1003", $"Cesium.Runtime doesn't exist under path '{RuntimePath.ItemSpec}'");
            success = false;
        }

        var missingAssemblyImportItems = ImportItems.Where(item => !File.Exists(item.ItemSpec)).ToList();
        foreach (var item in missingAssemblyImportItems)
        {
            ReportValidationError("CES1001", $"Imported assembly doesn't exist: '{item.ItemSpec}'");
            success = false;
        }

        if (!success) return false;

        options = new ValidatedOptions(
            CompilerExe: CompilerExe?.ItemSpec ?? throw new UnreachableException(),
            InputItems: InputFiles.Select(item => item.ItemSpec).ToArray(),
            OutputFile: OutputFile?.ItemSpec ?? throw new UnreachableException(),
            Namespace: Namespace,
            Framework: framework,
            Architecture: arch,
            ModuleKind: moduleKind,
            CoreLibPath: CoreLibPath?.ItemSpec,
            RuntimePath: RuntimePath?.ItemSpec,
            ImportItems: ImportItems.Select(item => item.ItemSpec).ToArray(),
            PreprocessorItems: PreprocessorItems.Select(item => item.ItemSpec).ToArray()
        );

        return true;
    }

    private IEnumerable<string> CollectCommandLineArguments(ValidatedOptions options)
    {
        yield return options.CompilerExe;
        yield return "--nologo";

        if (options.Framework is { } framework)
        {
            yield return "--framework";
            yield return framework.ToString();
        }

        if (options.Architecture is { } arch)
        {
            yield return "--arch";
            yield return arch.ToString();
        }

        if (options.ModuleKind is { } moduleKind)
        {
            yield return "--modulekind";
            yield return moduleKind.ToString();
        }

        if (!string.IsNullOrWhiteSpace(options.Namespace))
        {
            yield return "--namespace";
            yield return options.Namespace;
        }

        foreach (var import in options.ImportItems)
        {
            yield return "--import";
            yield return import;
        }

        if (!string.IsNullOrWhiteSpace(options.CoreLibPath))
        {
            yield return "--corelib";
            yield return options.CoreLibPath;
        }

        if (!string.IsNullOrWhiteSpace(options.RuntimePath))
        {
            yield return "--runtime";
            yield return options.RuntimePath;
        }

        foreach (var item in options.PreprocessorItems)
        {
            yield return "-D";
            yield return item;
        }

        yield return "--out";
        yield return $"\"{options.OutputFile}\"";

        foreach (var input in options.InputItems)
        {
            yield return $"\"{input}\"";
        }
    }

    private void ReportValidationError(string code, string message) =>
        BuildEngine.LogErrorEvent(new BuildErrorEventArgs(nameof(CesiumCompile), code, string.Empty, -1, -1, -1, -1, message, string.Empty, nameof(CesiumCompile)));

    private void ReportValidationWarning(string code, string message) =>
        BuildEngine.LogWarningEvent(new BuildWarningEventArgs(nameof(CesiumCompile), code, string.Empty, -1, -1, -1, -1, message, string.Empty, nameof(CesiumCompile)));

    private string GetResultingCommandLine(string executable, IReadOnlyCollection<string> arguments)
    {
        return $"{executable} {string.Join(" ", arguments)}";
    }

    private enum FrameworkKind
    {
        Net,
        NetFramework,
        NetStandard
    }

    private enum ArchitectureKind
    {
        Dynamic,
        Bit32,
        Bit64
    }

    private enum ModuleKind
    {
        Dll,
        Console,
        Windows,
        NetModule
    }

    private record ValidatedOptions(
        string CompilerExe,
        string[] InputItems,
        string OutputFile,
        string? Namespace,
        FrameworkKind? Framework,
        ArchitectureKind? Architecture,
        ModuleKind? ModuleKind,
        string? CoreLibPath,
        string? RuntimePath,
        string[] ImportItems,
        string[] PreprocessorItems
    );
}

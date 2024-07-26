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

// ReSharper disable once UnusedType.Global
public class CesiumCompile : Task
{
    [Required] public string CompilerExe { get; set; } = null!;
    [Required] public ITaskItem[] InputFiles { get; set; } = null!;
    [Required] public string OutputFile { get; set; } = null!;

    public string? Namespace { get; set; }
    public string? Framework { get; set; }
    public string? Architecture { get; set; }
    public string? ModuleType { get; set; }
    public string? CoreLibPath { get; set; }
    public string? RuntimePath { get; set; }
    public ITaskItem[] ImportItems { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] PreprocessorItems { get; set; } = Array.Empty<ITaskItem>();
    public bool DryRun = false;

    [Output] public string? ResultingCommandLine { get; private set; }
    [Output] public TaskItem[]? OutputFiles { get; private set; }

    public override bool Execute()
    {
        if (!TryValidate(out var options) || options is null)
        {
            return false;
        }

        var compilerProcess = new Process
        {
            StartInfo =
            {
                FileName = options.CompilerExe,
                Arguments = CollectCommandLineArguments(options),
                UseShellExecute = false,
            }
        };

        ResultingCommandLine = $"{compilerProcess.StartInfo.FileName} {compilerProcess.StartInfo.Arguments}";
        OutputFiles = [new TaskItem(OutputFile)];

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


        if (!string.IsNullOrWhiteSpace(CompilerExe) && !File.Exists(CompilerExe))
        {
            ReportValidationError("CES1000", $"Compiler executable doesn't exist under path '{CompilerExe}'");
            success = false;
        }

        var (isFrameworkValid, framework) = TryParseFramework(Framework);
        if (!isFrameworkValid)
        {
            var validValues =  Enum.GetValues(typeof(FrameworkKind)).Cast<FrameworkKind>().Select(kind => kind.ToString());
            ReportValidationError("CES1004", $"Framework should be in range: '{string.Join(", ", validValues)}', actual: '{Framework}'");
            success = false;
        }

        var (isArchValid, arch) = TryParseArchitectureKind(Architecture);
        if (!isArchValid)
        {
            var validValues =  Enum.GetValues(typeof(ArchitectureKind)).Cast<ArchitectureKind>().Select(kind => kind.ToString());
            ReportValidationError("CES1005", $"Architecture should be in range: '{string.Join(", ", validValues)}', actual: '{Architecture}'");
            success = false;
        }

        var (isModuleKindValid, moduleKind) = TryParseModuleKind(ModuleType);
        if (!isModuleKindValid)
        {
            var validValues = Enum.GetValues(typeof(ModuleKind)).Cast<ModuleKind>().Select(kind => kind.ToString());
            ReportValidationError("CES1006", $"ModuleKind should be in range: '{string.Join(", ", validValues)}', actual: '{ModuleType}'");
            success = false;
        }

        var missingCompileItems = InputFiles.Where(item => !File.Exists(item.ItemSpec)).ToList();
        foreach (var item in missingCompileItems)
        {
            ReportValidationError("CES1001", $"Source file doesn't exist: '{item.ItemSpec}'");
            success = false;
        }

        if (!string.IsNullOrWhiteSpace(CoreLibPath) && !File.Exists(CoreLibPath))
        {
            ReportValidationError("CES1002", $"CorLib doesn't exist under path '{CoreLibPath}'");
            success = false;
        }

        if (!string.IsNullOrWhiteSpace(RuntimePath) && !File.Exists(RuntimePath))
        {
            ReportValidationError("CES1003", $"Cesium.Runtime doesn't exist under path '{RuntimePath}'");
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
            CompilerExe: CompilerExe,
            InputItems: InputFiles.Select(item => item.ItemSpec).ToArray(),
            OutputFile: OutputFile,
            Namespace: Namespace,
            Framework: framework,
            Architecture: arch,
            ModuleKind: moduleKind,
            CoreLibPath: CoreLibPath,
            RuntimePath: RuntimePath,
            ImportItems: ImportItems.Select(item => item.ItemSpec).ToArray(),
            PreprocessorItems: PreprocessorItems.Select(item => item.ItemSpec).ToArray()
        );

        return true;
    }

    private string CollectCommandLineArguments(ValidatedOptions options)
    {
        var args = new List<string>();

        args.Add("--nologo");

        if (options.Framework is { } framework)
        {
            args.Add("--framework");
            args.Add(framework.ToString());
        }

        if (options.Architecture is { } arch)
        {
            args.Add("--arch");
            args.Add(arch.ToString());
        }

        if (options.ModuleKind is { } moduleKind)
        {
            args.Add("--modulekind");
            args.Add(moduleKind.ToString());
        }

        if (!string.IsNullOrWhiteSpace(options.Namespace))
        {
            args.Add("--namespace");
            args.Add(options.Namespace!);
        }

        foreach (var import in options.ImportItems)
        {
            args.Add("--import");
            args.Add(import);
        }

        if (!string.IsNullOrWhiteSpace(options.CoreLibPath))
        {
            args.Add("--corelib");
            args.Add(options.CoreLibPath!);
        }

        if (!string.IsNullOrWhiteSpace(options.RuntimePath))
        {
            args.Add("--runtime");
            args.Add(options.RuntimePath!);
        }

        foreach (var item in options.PreprocessorItems)
        {
            args.Add("-D");
            args.Add(item);
        }

        args.Add("--out");
        args.Add(options.OutputFile);

        foreach (var input in options.InputItems)
        {
            args.Add(input);
        }

        return ArgumentUtil.ToCommandLineString(args);
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

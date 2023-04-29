using System.Reflection;
using Cesium.CodeGen;
using Cesium.Compiler;
using Cesium.Core;
using CommandLine;
using CommandLine.Text;
using Mono.Cecil;

var parserResult = new Parser(x => x.HelpWriter = null).ParseArguments<Arguments>(args);

return await parserResult.MapResult(async args =>
    {
        if (!args.NoLogo)
        {
            Console.WriteLine($"Cesium v{Assembly.GetExecutingAssembly().GetName().Version}");
        }

        if (args.InputFilePaths.Count == 0)
        {
            Console.Error.WriteLine("Input file paths should be defined.");
            return 2;
        }

        var targetArchitectureSet = args.TargetArchitectureSet;
        var targetRuntime = args.Framework switch
        {
            TargetFrameworkKind.NetFramework => TargetRuntimeDescriptor.Net48,
            TargetFrameworkKind.NetStandard => TargetRuntimeDescriptor.NetStandard20,
            _ => TargetRuntimeDescriptor.Net60
        };

        var cesiumRuntime = args.CesiumCRuntime ?? Path.Combine(AppContext.BaseDirectory, "Cesium.Runtime.dll");
        var defaultImportsAssembly = args.DefaultImportAssemblies ?? Array.Empty<string>();
#pragma warning disable IL3000 // Automatic discovery of corelib is fallback option, if tooling do not pass that parameter
        var corelibAssembly = args.CoreLib ?? typeof(Math).Assembly.Location; // System.Runtime.dll
#pragma warning restore IL3000
        var moduleKind = args.ModuleKind ?? Path.GetExtension(args.OutputFilePath).ToLowerInvariant() switch
        {
            ".exe" => ModuleKind.Console,
            ".dll" => ModuleKind.Dll,
            var o => throw new CompilationException($"Unknown file extension: {o}. \"modulekind\" is not specified.")
        };
        var compilationOptions = new CompilationOptions(
            targetRuntime,
            targetArchitectureSet,
            moduleKind,
            corelibAssembly,
            cesiumRuntime,
            defaultImportsAssembly,
            args.Namespace,
            args.GlobalClass,
            args.DefineConstant.ToList());
        return await Compilation.Compile(args.InputFilePaths, args.OutputFilePath, compilationOptions);
    },
    _ =>
    {
        string helpText = PrepareHelpText(parserResult);
        Console.WriteLine(helpText);
        return Task.FromResult(-1);
    });

static string PrepareHelpText<T>(ParserResult<T> result)
{
    if (result is NotParsed<T> notParsed && notParsed.Errors.IsVersion())
        return HelpText.AutoBuild(result);

    var helpText = HelpText.AutoBuild(result, h =>
    {
        h.AddEnumValuesToHelpText = true;
        return HelpText.DefaultParsingErrorsHandler(result, h);
    }, e => e);

    return helpText;
}

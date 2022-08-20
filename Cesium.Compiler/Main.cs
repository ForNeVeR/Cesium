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

        var targetRuntime = args.Framework switch
        {
            TargetFrameworkKind.NetFramework => TargetRuntimeDescriptor.Net48,
            TargetFrameworkKind.NetStandard => TargetRuntimeDescriptor.NetStandard20,
            _ => TargetRuntimeDescriptor.Net60
        };

        var cesiumRuntime = args.CesiumCRuntime ?? Path.Combine(AppContext.BaseDirectory, "Cesium.Runtime.dll");
        var defaultImportsAssembly = args.DefaultImportAssemblies ?? Array.Empty<string>();
        var corelibAssembly = args.CoreLib ?? typeof(Math).Assembly.Location; // System.Runtime.dll
        var moduleKind = args.ModuleKind ?? Path.GetExtension(args.OutputFilePath).ToLowerInvariant() switch
        {
            ".exe" => ModuleKind.Console,
            ".dll" => ModuleKind.Dll,
            var o => throw new CompilationException($"Unknown file extension: {o}. \"modulekind\" is not specified.")
        };
        var compilationOptions = new CompilationOptions(targetRuntime, moduleKind, corelibAssembly, cesiumRuntime, defaultImportsAssembly, args.Namespace, args.GlobalClass);
        return await Compilation.Compile(args.InputFilePaths, args.OutputFilePath, compilationOptions);
    },
    _ =>
    {
        DisplayHelp(parserResult);
        return Task.FromResult(-1);
    });

static void DisplayHelp<T>(ParserResult<T> result)
{
    var helpText = HelpText.AutoBuild(result, h =>
    {
        h.AddEnumValuesToHelpText = true;
        return HelpText.DefaultParsingErrorsHandler(result, h);
    }, e => e);
    Console.WriteLine(helpText);
}

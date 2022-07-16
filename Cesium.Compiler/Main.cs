using System.Reflection;
using Cesium.CodeGen;
using Cesium.Compiler;
using CommandLine;

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

        return await Compilation.Compile(args.InputFilePaths, args.OutputFilePath, targetRuntime);
    },
    _ =>
    {
        DisplayHelp(parserResult);
        return Task.FromResult(-1);
    });

static void DisplayHelp<T>(ParserResult<T> result)
{
    var helpText = CommandLine.Text.HelpText.AutoBuild(result, h =>
    {
        h.AddEnumValuesToHelpText = true;
        return h;
    }, e => e);
    Console.WriteLine(helpText);
}

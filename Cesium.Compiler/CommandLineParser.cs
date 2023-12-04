using System.Reflection;
using Cesium.CodeGen;
using CommandLine;
using CommandLine.Text;

namespace Cesium.Compiler;

public class CommandLineParser
{
    public static async Task<int> ParseCommandLineArgs(string[] args, ICompilerReporter reporter, Func<Arguments, Task<int>> worker)
    {
        var parserResult = new CommandLine.Parser(x =>
        {
            x.HelpWriter = null;
            x.AllowMultiInstance = true;
        }).ParseArguments<Arguments>(args);

        return await parserResult.MapResult(async args =>
        {
            if (!args.NoLogo && !args.ProducePreprocessedFile)
            {
                reporter.ReportInformation($"Cesium v{Assembly.GetExecutingAssembly().GetName().Version}");
            }

            if (args.InputFilePaths.Count == 0)
            {
                reporter.ReportError("Input file paths should be defined.");
                return 2;
            }

            if (!args.ProducePreprocessedFile && string.IsNullOrWhiteSpace(args.OutputFilePath))
            {
                reporter.ReportError("Required option 'o, out' is missing.");
                return 3;
            }

            return await worker(args);
        },
        _ =>
        {
            string helpText = PrepareHelpText(parserResult);
            Console.WriteLine(helpText);
            return Task.FromResult(-1);
        });
    }

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
}

using System.Reflection;
using System.Text;
using Cesium.CodeGen;
using Cesium.CodeGen.Generators;
using Cesium.Compiler;
using Cesium.Parser;
using Cesium.Preprocessor;
using CommandLine;
using Mono.Cecil;
using Yoakke.C.Syntax;
using Yoakke.Streams;

static Task<string> Preprocess(string compilationFileDirectory, TextReader reader)
{
    var currentProcessPath = Path.GetDirectoryName(Environment.ProcessPath)
                             ?? throw new Exception("Cannot determine path to the compiler executable.");

    var stdLibDirectory = Path.Combine(currentProcessPath, "stdlib");
    var includeContext = new FileSystemIncludeContext(stdLibDirectory, compilationFileDirectory);
    var preprocessorLexer = new CPreprocessorLexer(reader);
    var preprocessor = new CPreprocessor(preprocessorLexer, includeContext);
    return preprocessor.ProcessSource();
}

return await Parser.Default.ParseArguments<Arguments>(args).MapResult(async args =>
    {
        if (!args.NoLogo)
        {
            Console.WriteLine($"Cesium v{Assembly.GetExecutingAssembly().GetName().Version}");
        }

        if (args.InputFilePath == null)
        {
            Console.Error.WriteLine($"Input file path should be set.");
            return 2;
        }

        if (args.OutputFilePath == null)
        {
            Console.Error.WriteLine($"Output file path should be set.");
            return 2;
        }

        if (!File.Exists(args.InputFilePath))
        {
            Console.Error.WriteLine($"File {args.InputFilePath} not found.");
            return 2;
        }

        await using var input = new FileStream(args.InputFilePath, FileMode.Open);
        using var reader = new StreamReader(input, Encoding.UTF8);

        Console.WriteLine($"Processing input file {args.InputFilePath}.");
        var compilationFileDirectory = Path.GetDirectoryName(args.InputFilePath);
        var content = await Preprocess(string.IsNullOrEmpty(compilationFileDirectory) ? Environment.CurrentDirectory : compilationFileDirectory, reader);
        var lexer = new CLexer(content);
        var parser = new CParser(lexer);
        var translationUnitParseError = parser.ParseTranslationUnit();
        if (translationUnitParseError.IsError)
        {
            switch (translationUnitParseError.Error.Got)
            {
                case CToken token:
                    throw new Exception($"Error during parsing {args.InputFilePath}. Error at position {translationUnitParseError.Error.Position}. Got {token.LogicalText}.");
                case char ch:
                    throw new Exception($"Error during parsing {args.InputFilePath}. Error at position {translationUnitParseError.Error.Position}. Got {ch}.");
                default:
                    throw new Exception($"Error during parsing {args.InputFilePath}. Error at position {translationUnitParseError.Error.Position}.");
            }
        }

        var translationUnit = translationUnitParseError.Ok.Value;

        if (parser.TokenStream.Peek().Kind != CTokenType.End)
            throw new Exception($"Excessive output after the end of a translation unit at {lexer.Position}.");

        var assemblyName = Path.GetFileNameWithoutExtension(args.OutputFilePath);
        var moduleKind = Path.GetExtension(args.OutputFilePath).ToLowerInvariant() switch
        {
            ".exe" => ModuleKind.Console,
            ".dll" => ModuleKind.Dll,
            var o => throw new Exception($"Unknown file extension: {o}.")
        };

        var targetRuntime = args.Framework switch
        {
            TargetFrameworkKind.NetFramework => TargetRuntimeDescriptor.Net48,
            TargetFrameworkKind.NetStandard => TargetRuntimeDescriptor.NetStandard20,
            _ => TargetRuntimeDescriptor.Net60
        };

        Console.WriteLine($"Generating assembly {args.OutputFilePath}.");
        var defaultImportAssemblies = new []
        { 
            typeof(Math).Assembly, // System.Runtime.dll
            typeof(Console).Assembly, // System.Console.dll
            typeof(Cesium.Runtime.StdLibFunctions).Assembly
        };
        var assembly = Assemblies.Generate(
            translationUnit,
            new AssemblyNameDefinition(assemblyName, new Version()),
            moduleKind,
            targetRuntime,
            defaultImportAssemblies);
        assembly.Write(args.OutputFilePath);

        if (moduleKind == ModuleKind.Console && args.Framework == TargetFrameworkKind.Net)
        {
            var runtimeConfigFilePath = Path.ChangeExtension(args.OutputFilePath, "runtimeconfig.json");
            Console.WriteLine($"Generating a .NET 6 runtime config at {runtimeConfigFilePath}.");
            File.WriteAllText(runtimeConfigFilePath, RuntimeConfig.EmitNet6());
        }

        return 0;
    },
    _ => Task.FromResult(-1));

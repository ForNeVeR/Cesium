using System.Reflection;
using System.Text;
using Cesium.CodeGen;
using Cesium.CodeGen.Generators;
using Cesium.Compiler;
using Cesium.Parser;
using CommandLine;
using Mono.Cecil;
using Yoakke.C.Syntax;
using Yoakke.Streams;

Console.WriteLine($"Cesium v{Assembly.GetExecutingAssembly().GetName().Version}");

return Parser.Default.ParseArguments<Arguments>(args).MapResult(args =>
    {
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

        using var input = new FileStream(args.InputFilePath, FileMode.Open);
        using var reader = new StreamReader(input, Encoding.UTF8);

        Console.WriteLine($"Processing input file {args.InputFilePath}.");
        var lexer = new CLexer(reader);
        var parser = new CParser(lexer);
        var translationUnit = parser.ParseTranslationUnit().Ok.Value;

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
        var assembly = Assemblies.Generate(
            translationUnit,
            new AssemblyNameDefinition(assemblyName, new Version()),
            moduleKind,
            targetRuntime);
        assembly.Write(args.OutputFilePath);

        if (moduleKind == ModuleKind.Console && args.Framework == TargetFrameworkKind.Net)
        {
            var runtimeConfigFilePath = Path.ChangeExtension(args.OutputFilePath, "runtimeconfig.json");
            Console.WriteLine($"Generating a .NET 6 runtime config at {runtimeConfigFilePath}.");
            File.WriteAllText(runtimeConfigFilePath, RuntimeConfig.EmitNet6());
        }

        return 0;
    },
    _ => -1);

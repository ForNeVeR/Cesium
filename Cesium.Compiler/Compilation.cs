using System.Text;
using Cesium.CodeGen;
using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Cesium.Parser;
using Cesium.Preprocessor;
using Mono.Cecil;
using Yoakke.Streams;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Compiler;

internal static class Compilation
{
    public static async Task<int> Compile(
        IEnumerable<string> inputFilePaths,
        string outputFilePath,
        CompilationOptions compilationOptions)
    {
        Console.WriteLine($"Generating assembly {outputFilePath}.");
        var assemblyContext = CreateAssembly(outputFilePath, compilationOptions);

        foreach (var inputFilePath in inputFilePaths)
        {
            Console.WriteLine($"Processing input file \"{inputFilePath}\".");
            await GenerateCode(assemblyContext, inputFilePath);
        }

        SaveAssembly(assemblyContext, compilationOptions.TargetRuntime.Kind, outputFilePath, compilationOptions.CesiumRuntime);

        return 0;
    }

    private static AssemblyContext CreateAssembly(string outputFilePath, CompilationOptions compilationOptions)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(outputFilePath);
        return AssemblyContext.Create(
            new AssemblyNameDefinition(assemblyName, new Version()),
            compilationOptions);
    }

    private static Task<string> Preprocess(string compilationSourcePath, string compilationFileDirectory, TextReader reader, CompilationOptions compilationOptions)
    {
        var currentProcessPath = Path.GetDirectoryName(Environment.ProcessPath)
                                 ?? throw new Exception("Cannot determine path to the compiler executable.");

        var stdLibDirectory = Path.Combine(currentProcessPath, "stdlib");
        var includeContext = new FileSystemIncludeContext(stdLibDirectory, compilationFileDirectory);
        var preprocessorLexer = new CPreprocessorLexer(reader);
        var definesContext = new InMemoryDefinesContext();
        var outOfFileRange = new Yoakke.SynKit.Text.Range();
        foreach (var define in compilationOptions.DefineConstants)
        {
            definesContext.DefineMacro(define, null, new[] { new Token<CPreprocessorTokenType>(outOfFileRange, "1", CPreprocessorTokenType.PreprocessingToken) });
        }

        var preprocessor = new CPreprocessor(compilationSourcePath, preprocessorLexer, includeContext, definesContext);
        return preprocessor.ProcessSource();
    }

    private static async Task GenerateCode(AssemblyContext context, string inputFilePath)
    {
        var compilationFileDirectory = Path.GetDirectoryName(inputFilePath)!;
        var compilationSourcePath = Path.GetFullPath(inputFilePath);

        using var reader = new StreamReader(inputFilePath, Encoding.UTF8);

        var content = await Preprocess(compilationSourcePath, compilationFileDirectory, reader, context.CompilationOptions);
        var lexer = new CLexer(content);
        var parser = new CParser(lexer);
        var translationUnitParseError = parser.ParseTranslationUnit();
        if (translationUnitParseError.IsError)
        {
            switch (translationUnitParseError.Error.Got)
            {
                case CToken token:
                    throw new ParseException($"Error during parsing {inputFilePath}. Error at position {translationUnitParseError.Error.Position}. Got {token.LogicalText}.");
                case char ch:
                    throw new ParseException($"Error during parsing {inputFilePath}. Error at position {translationUnitParseError.Error.Position}. Got {ch}.");
                default:
                    throw new ParseException($"Error during parsing {inputFilePath}. Error at position {translationUnitParseError.Error.Position}.");
            }
        }

        var translationUnit = translationUnitParseError.Ok.Value;

        var firstUnprocessedToken = parser.TokenStream.Peek();
        if (firstUnprocessedToken.Kind != CTokenType.End)
            throw new ParseException($"Excessive output after the end of a translation unit {inputFilePath} at {lexer.Position}. Next token {firstUnprocessedToken.Text}.");

        var translationUnitName = Path.GetFileNameWithoutExtension(inputFilePath);
        context.EmitTranslationUnit(translationUnitName, translationUnit);
    }

    private static void SaveAssembly(
        AssemblyContext context,
        SystemAssemblyKind targetFrameworkKind,
        string outputFilePath,
        string compilerRuntimeDll)
    {
        context.VerifyAndGetAssembly().Write(outputFilePath);

        // This part should go to Cesium.SDK eventually together with
        // runtimeconfig.json generation
        var outputExecutablePath = Path.GetDirectoryName(outputFilePath) ?? Environment.CurrentDirectory;
        var applicationRuntime = Path.Combine(outputExecutablePath, "Cesium.Runtime.dll");

        // Prevent copying of the Cesium.Runtime if compile in same directory as compiler.
        if (!string.Equals(Path.GetFullPath(compilerRuntimeDll), Path.GetFullPath(applicationRuntime), StringComparison.InvariantCultureIgnoreCase))
        {
            File.Copy(compilerRuntimeDll, applicationRuntime, true);
        }

        if (context.Module.Kind == ModuleKind.Console && targetFrameworkKind == SystemAssemblyKind.SystemRuntime)
        {
            var runtimeConfigFilePath = Path.ChangeExtension(outputFilePath, "runtimeconfig.json");
            Console.WriteLine($"Generating a .NET 6 runtime config at {runtimeConfigFilePath}.");
            File.WriteAllText(runtimeConfigFilePath, RuntimeConfig.EmitNet6());
        }
    }
}

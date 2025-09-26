// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text;
using Cesium.Ast;
using Cesium.CodeGen;
using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Cesium.Parser;
using Cesium.Preprocessor;
using Mono.Cecil;
using TruePath;
using Yoakke.Streams;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Compiler;

internal static class Compilation
{
    public static async Task<int> Compile(
        IEnumerable<LocalPath> inputFilePaths,
        LocalPath outputFile,
        CompilationOptions compilationOptions)
    {
        if (compilationOptions.ProducePreprocessedFile)
        {
            foreach (var inputFilePath in inputFilePaths)
            {
                var content = await Preprocess(inputFilePath, compilationOptions);
                Console.WriteLine(content);
            }

            return 0;
        }

        if (compilationOptions.ProduceAstFile)
        {
            foreach (var inputFilePath in inputFilePaths)
            {
                var translationUnit = await CreateAst(
                    compilationOptions,
                    inputFilePath.ResolveToCurrentDirectory());
                DumpAst(translationUnit);
            }

            return 0;
        }

        Console.WriteLine($"Generating assembly \"{outputFile.Value}\".");

        var assemblyContext = CreateAssembly(outputFile.ResolveToCurrentDirectory(), compilationOptions);

        var inputSources = new List<AbsolutePath>();
        foreach (var inputFile in inputFilePaths)
        {
            if (JsonObjectFile.IsSupportedExtension(inputFile))
            {
                Console.WriteLine($"Processing object file \"{inputFile.Value}\".");
                var objectFile = await JsonObjectFile.Read(inputFile.ResolveToCurrentDirectory());
                if (objectFile.CompilationOptions != compilationOptions)
                {
                    throw new InvalidOperationException(
                        $"Compilation options differ between the current compilation session and compilation session of file \"{objectFile}\". I will not proceed.");
                }

                inputSources.AddRange(
                    objectFile.InputFilePaths.Select(x => new LocalPath(x).ResolveToCurrentDirectory()));
                continue;
            }

            inputSources.Add(inputFile.ResolveToCurrentDirectory());
        }

        foreach (var sourceFile in inputSources)
        {
            Console.WriteLine($"Processing source file \"{sourceFile.Value}\".");
            await GenerateCode(assemblyContext, sourceFile);
        }

        SaveAssembly(
            assemblyContext,
            compilationOptions.TargetRuntime.Kind,
            outputFile.ResolveToCurrentDirectory(),
            compilationOptions.CesiumRuntime.ResolveToCurrentDirectory());

        return 0;
    }

    private static void DumpAst(TranslationUnit translationUnit)
    {
        var astDumper = new AstDumper(Console.Out);
        astDumper.Dump(translationUnit);
    }

    private static AssemblyContext CreateAssembly(AbsolutePath outputFile, CompilationOptions compilationOptions)
    {
        var assemblyName = outputFile.GetFilenameWithoutExtension();
        return AssemblyContext.Create(
            new AssemblyNameDefinition(assemblyName, new Version()),
            compilationOptions);
    }

    private static Task<string> Preprocess(AbsolutePath compilationSource, AbsolutePath compilationFileDirectory, TextReader reader, CompilationOptions compilationOptions)
    {
        var currentProcessDir = new AbsolutePath(AppContext.BaseDirectory);

        var stdLibDirectory = currentProcessDir / "stdlib";
        var includeDirectories = new[] { compilationFileDirectory }
            .Concat(compilationOptions.AdditionalIncludeDirectories.Select(x => x.ResolveToCurrentDirectory()))
            .ToImmutableArray();
        var includeContext = new FileSystemIncludeContext(stdLibDirectory, includeDirectories);
        var preprocessorLexer = new CPreprocessorLexer(new SourceFile(compilationSource.Value, reader));
        var definesContext = new InMemoryDefinesContext();
        var outOfFileRange = new Range();
        foreach (var define in compilationOptions.DefineConstants)
        {
            definesContext.DefineMacro(
                define,
                parameters: null,
                replacement:
                [
                    new Token<CPreprocessorTokenType>(outOfFileRange, new(), "1", CPreprocessorTokenType.PreprocessingToken)
                ]);
        }

        var preprocessor = new CPreprocessor(
            compilationSource,
            preprocessorLexer,
            includeContext,
            definesContext,
            new WarningProcessor());
        return preprocessor.ProcessSource();
    }

    private static async Task<string> Preprocess(LocalPath source, CompilationOptions compilationOptions)
    {
        var compilationFileDirectory = source.Parent
            ?? throw new CompilationException($"Cannot determine parent directory of file \"{source.Value}\".");
        var compilationSourcePath = source.ResolveToCurrentDirectory().Canonicalize();

        using var reader = new StreamReader(source.Value, Encoding.UTF8);

        var content = await Preprocess(
            compilationSourcePath,
            compilationFileDirectory.ResolveToCurrentDirectory(),
            reader,
            compilationOptions);
        return content;
    }

    private static async Task GenerateCode(AssemblyContext context, AbsolutePath inputFile)
    {
        TranslationUnit translationUnit = await CreateAst(context.CompilationOptions, inputFile);

        var translationUnitName = inputFile.GetFilenameWithoutExtension();
        context.EmitTranslationUnit(translationUnitName, translationUnit);
    }

    private static async Task<TranslationUnit> CreateAst(CompilationOptions compilationOptions, AbsolutePath inputFile)
    {
        var content = await Preprocess(inputFile, compilationOptions);
        var lexer = new CLexer(content);
        var parser = new CParser(lexer);
        var translationUnitParseError = parser.ParseTranslationUnit();
        if (translationUnitParseError.IsError)
        {
            throw translationUnitParseError.Error.Got switch
            {
                CToken token => new ParseException($"Error during parsing {inputFile}. Error at position {translationUnitParseError.Error.Position}. Got {token.LogicalText}."),
                char ch => new ParseException($"Error during parsing {inputFile}. Error at position {translationUnitParseError.Error.Position}. Got {ch}."),
                _ => new ParseException($"Error during parsing {inputFile}. Error at position {translationUnitParseError.Error.Position}."),
            };
        }

        var translationUnit = translationUnitParseError.Ok.Value;

        var firstUnprocessedToken = parser.TokenStream.Peek();
        if (firstUnprocessedToken.Kind != CTokenType.End)
            throw new ParseException($"Excessive output after the end of a translation unit {inputFile} at {lexer.Position}. Next token {firstUnprocessedToken.Text}.");
        return translationUnit;
    }

    private static void SaveAssembly(
        AssemblyContext context,
        SystemAssemblyKind targetFrameworkKind,
        AbsolutePath outputFilePath,
        AbsolutePath compilerRuntimeDll)
    {
        context.VerifyAndGetAssembly().Write(outputFilePath.Value);

        // This part should go to Cesium.SDK eventually together with
        // runtimeconfig.json generation
        var outputExecutablePath = outputFilePath.Parent ?? AbsolutePath.CurrentWorkingDirectory;
        var applicationRuntime = outputExecutablePath / "Cesium.Runtime.dll";

        // Prevent copying of the Cesium.Runtime if compile in same directory as compiler.
        if (compilerRuntimeDll.Canonicalize() != applicationRuntime.Canonicalize())
        {
            File.Copy(compilerRuntimeDll.Value, applicationRuntime.Value, overwrite: true);
        }

        if (context.Module.Kind == ModuleKind.Console && targetFrameworkKind == SystemAssemblyKind.SystemRuntime)
        {
            var runtimeConfigFilePath = Path.ChangeExtension(outputFilePath.Value, "runtimeconfig.json");
            Console.WriteLine($"Generating a .NET 6 runtime config at {runtimeConfigFilePath}.");
            File.WriteAllText(runtimeConfigFilePath, RuntimeConfig.EmitNet6());
        }
    }
}

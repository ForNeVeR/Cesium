using System.Diagnostics.CodeAnalysis;
using Cesium.CodeGen;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.Compiler;

public static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Arguments))]
    public static async Task<int> Main(string[] args)
    {
        return await CommandLineParser.ParseCommandLineArgs(args, new CompilerReporter(), async args =>
        {
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
            var moduleKind = args.ProducePreprocessedFile ? ModuleKind.Console : args.ModuleKind ?? Path.GetExtension(args.OutputFilePath).ToLowerInvariant() switch
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
                args.DefineConstant.ToList(),
                args.IncludeDirectories.ToList(),
                args.ProducePreprocessedFile);
            return await Compilation.Compile(args.InputFilePaths, args.OutputFilePath, compilationOptions);
        });
    }
}

class CompilerReporter : ICompilerReporter
{
    public void ReportError(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void ReportInformation(string message)
    {
        Console.Out.WriteLine(message);
    }
}

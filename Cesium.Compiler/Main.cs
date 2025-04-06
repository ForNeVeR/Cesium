// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Cesium.CodeGen;
using Cesium.Core;
using Mono.Cecil;
using TruePath;

namespace Cesium.Compiler;

public static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Arguments))]
    public static async Task<int> Main(string[] args)
    {
        return await CommandLineParser.ParseCommandLineArgs(args, new CompilerReporter(), async options =>
        {
            var targetArchitectureSet = options.TargetArchitectureSet;
            var targetRuntime = options.Framework switch
            {
                TargetFrameworkKind.NetFramework => TargetRuntimeDescriptor.Net48,
                TargetFrameworkKind.NetStandard => TargetRuntimeDescriptor.NetStandard20,
                _ => TargetRuntimeDescriptor.Net60
            };

            var cesiumRuntime = options.CesiumCRuntime ?? Path.Combine(AppContext.BaseDirectory, "Cesium.Runtime.dll");
            var defaultImportsAssembly = options.DefaultImportAssemblies.Select(x => new LocalPath(x));
#pragma warning disable IL3000 // Automatic discovery of corelib is fallback option, if tooling do not pass that parameter
            var corelibAssembly = options.CoreLib ?? typeof(Math).Assembly.Location; // System.Runtime.dll
#pragma warning restore IL3000
            var moduleKind = (options.ProducePreprocessedFile || options.DumpAst || options.ProduceObjectFileImitation) ? ModuleKind.Console : options.ModuleKind ?? Path.GetExtension(options.OutputFilePath).ToLowerInvariant() switch
            {
                ".exe" => ModuleKind.Console,
                ".dll" => ModuleKind.Dll,
                var o => throw new CompilationException($"Unknown file extension: {o}. \"modulekind\" is not specified.")
            };
            var compilationOptions = new CompilationOptions(
                targetRuntime,
                targetArchitectureSet,
                moduleKind,
                new LocalPath(corelibAssembly),
                new LocalPath(cesiumRuntime),
                defaultImportsAssembly.ToList(),
                options.Namespace,
                options.GlobalClass,
                options.DefineConstant.ToList(),
                options.IncludeDirectories.Select(x => new LocalPath(x)).ToList(),
                options.ProducePreprocessedFile,
                options.DumpAst);

            if (options.ProduceObjectFileImitation)
            {
                await JsonObjectFile.Write(
                    options.InputFilePaths.Select(x => new LocalPath(x)),
                    compilationOptions,
                    AbsolutePath.CurrentWorkingDirectory / options.OutputFilePath);
                return 0;
            }

            return await Compilation.Compile(
                options.InputFilePaths.Select(x => new LocalPath(x)),
                new LocalPath(options.OutputFilePath),
                compilationOptions);
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

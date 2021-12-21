using Cesium.CodeGen.Generators;
using Cesium.Parser;
using Cesium.Test.Framework;
using Mono.Cecil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Tests;

public abstract class CodeGenTestBase : VerifyTestBase
{
    protected static AssemblyDefinition GenerateAssembly(string source, TargetRuntimeDescriptor? targetRuntime)
    {
        var translationUnit = new CParser(new CLexer(source)).ParseTranslationUnit().Ok.Value;
        var assembly = Assemblies.Generate(
            translationUnit,
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console,
            targetRuntime);

        // To resolve IL labels:
        using (var stream = new MemoryStream())
            assembly.Write(stream);

        return assembly;
    }
}

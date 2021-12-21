using System.Text;
using Cesium.Parser;
using Cesium.Test.Framework;
using Mono.Cecil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Tests;

public class CodeGenTests : VerifyTestBase
{
    private static AssemblyDefinition GenerateAssembly(
        string source, Generator.TargetRuntimeIdentifier? targetRuntime)
    {
        var translationUnit = new CParser(new CLexer(source)).ParseTranslationUnit().Ok.Value;
        var assembly = Generator.GenerateAssembly(
            translationUnit,
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console,
            targetRuntime);

        // To resolve IL labels:
        using (var stream = new MemoryStream())
            assembly.Write(stream);

        return assembly;
    }

    private static Task DoTargetRuntimeTest(Generator.TargetRuntimeIdentifier? targetRuntime)
    {
        var assembly = GenerateAssembly("int main() {}", targetRuntime);
        var verify = Verify(assembly.MainModule.TypeSystem.CoreLibrary.ToString());
        return targetRuntime != null ?
            verify.UseParameters(targetRuntime.targetFramework, targetRuntime.version) : verify;
    }

    private static Task VerifyMethods(TypeDefinition type)
    {
        var result = new StringBuilder();
        foreach (var method in type.Methods)
        {
            result.AppendLine(method.ToString());
            foreach (var instruction in method.Body.Instructions)
                result.AppendLine($"  {instruction}");
        }

        return Verify(result);
    }

    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(source, default);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task DefaultedFrameworkTest() =>
        DoTargetRuntimeTest(default);

    [Theory]
    [InlineData(Generator.TargetFrameworks.mscorlib, "4.0.0.0")]
    [InlineData(Generator.TargetFrameworks.SystemRuntime, "4.2.2.0")]
    [InlineData(Generator.TargetFrameworks.netstandard, "2.1.0.0")]
    public Task FrameworkTest(Generator.TargetFrameworks targetFramework, string versionString) =>
        DoTargetRuntimeTest(new Generator.TargetRuntimeIdentifier(
            targetFramework, new Version(versionString)));

    [Fact]
    public Task EmptyMainTest() => DoTest("int main() {}");

    [Fact]
    public Task ArithmeticMainTest() => DoTest("int main() { return 2 + 2 * 2; }");
}

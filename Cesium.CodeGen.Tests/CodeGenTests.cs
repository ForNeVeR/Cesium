using System.Text;
using Cesium.Parser;
using Cesium.Test.Framework;
using Mono.Cecil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Tests;

public class CodeGenTests : VerifyTestBase
{
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
        var translationUnit = new CParser(new CLexer(source)).ParseTranslationUnit().Ok.Value;
        var assembly = Generator.GenerateAssembly(
            translationUnit,
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console);

        // To resolve IL labels:
        using (var stream = new MemoryStream())
            assembly.Write(stream);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EmptyMainTest() => DoTest("int main() {}");

    [Fact]
    public Task ArithmeticMainTest() => DoTest("int main() { return 2 + 2 * 2; }");
}

using System.Text;
using Mono.Cecil;

namespace Cesium.CodeGen.Tests;

public class CodeGenTests : CodeGenTestBase
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
        var assembly = GenerateAssembly(source, default);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EmptyMainTest() => DoTest("int main() {}");

    [Fact]
    public Task ArithmeticMainTest() => DoTest("int main() { return 2 + 2 * 2; }");
}
